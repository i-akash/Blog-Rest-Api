using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Blog_Rest_Api.DTOModels;
using Blog_Rest_Api.Persistent_Model;
using Blog_Rest_Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace Blog_Rest_Api.Repositories{
    class StoriesRepository : IStoriesRepository
    {
        private BlogContext blogContext;
        private readonly IMapper mapper;

        public StoriesRepository(BlogContext blogContext,IMapper mapper)
        {
           this.blogContext=blogContext;
            this.mapper = mapper;
        }

        public async Task<DBStatus> AddStoryAsync(RequestStoryDTO storyDTO,string userId)
        {
            

            Story story=mapper.Map<Story>(storyDTO);
            story.AuthorId=userId;
            blogContext.Stories.Add(story);
            var resultStatus=await blogContext.SaveChangesAsync();
            return resultStatus==0 ? DBStatus.Failed : DBStatus.Added;
        }

        public async Task<List<ResponseStoryDTO>> GetStoryAsync()
        {
            return await blogContext.Stories
                                    .Include(story=>story.Author)
                                    .AsNoTracking()
                                    .Select(story=>mapper.Map<ResponseStoryDTO>(story))
                                    .ToListAsync();
        }

        public async Task<List<ResponseStoryDTO>> GetStoryAsync(int skip,int top)
        {
            return await blogContext.Stories
                                    .Include(story=>story.Author)
                                    .AsNoTracking()
                                    .Skip(skip)
                                    .Take(top)
                                    .Select(story=>mapper.Map<ResponseStoryDTO>(story))
                                    .ToListAsync();
        }

        public async Task<ResponseStoryDTO> GetStoryAsync(Guid storyId)
        {
            return await blogContext.Stories.Include(story=>story.Author)
                                            .AsNoTracking()
                                            .Select(story=>mapper.Map<ResponseStoryDTO>(story)).FirstOrDefaultAsync(story=>story.StoryId==storyId);
        }

        public async Task<DBStatus> ReplaceStoryAsync(RequestStoryDTO storyDTO,string userId)
        {
            Story persistentStory=await blogContext.Stories.FirstOrDefaultAsync(s=>s.StoryId==storyDTO.StoryId);
           
            if(persistentStory==null)
                return DBStatus.NotFound;

            if(userId!=persistentStory.AuthorId)
                return DBStatus.Forbidden;
            
            persistentStory.Title=storyDTO.Title;
            persistentStory.Body=storyDTO.Body;    
            persistentStory.PublishedDate=storyDTO.PublishedDate;

            var resultStatus=await blogContext.SaveChangesAsync();
            return resultStatus==0 ? DBStatus.NotModified : DBStatus.Modified ;
        }

        public async Task<DBStatus> RemoveStoryAsync(Guid storyId,string userId){
            Story persistentStory=await blogContext.Stories.AsNoTracking().FirstOrDefaultAsync(s=>s.StoryId==storyId);
            
            if(persistentStory==null)
                return DBStatus.NotFound;
            
            if(userId!=persistentStory.AuthorId)
                return DBStatus.Forbidden;
                
            blogContext.Stories.Remove(persistentStory);
            var resultStatus=await blogContext.SaveChangesAsync();
            return resultStatus==0 ? DBStatus.NotDeleted : DBStatus.Deleted ;
        }

    }
}