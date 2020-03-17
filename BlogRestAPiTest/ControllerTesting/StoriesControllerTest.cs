﻿using Blog_Rest_Api.Controllers;
using Blog_Rest_Api.Services;
using Blog_Rest_Api.DTOModels;
using Blog_Rest_Api.Utils;
using Xunit;
using Moq;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Xunit.Abstractions;
using BlogRestAPiTest.Data;

namespace BlogRestAPiTest.ControllerTesting
{
    public class StoriesControllerTest
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly Mock<IStoriesService> storiesService;
        private StoriesController storiesController;
        private readonly ControllerContext httpContext;

        public ControllerContext CreateHttpContext(List<Claim> claims)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims);
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(claimsIdentity)
                }
            };
        }

        public StoriesControllerTest(ITestOutputHelper testOutputHelper)
        {
            Claim claim = new Claim(ClaimTypes.Sid, "akash");
            List<Claim> claims = new List<Claim> { claim };
            this.testOutputHelper = testOutputHelper;

            httpContext = CreateHttpContext(claims);
            storiesService = new Mock<IStoriesService>();
            storiesController = new StoriesController(storiesService.Object);


        }




        [Theory]
        [ClassData(typeof(RequestStoriesTestData))]
        public async Task TestCreateStory_BadRequest(RequestStoryDTO requestStoryDTO)
        {

            // Arrange
            DBStatus dbStatus = DBStatus.Failed;

            storiesService.Setup(x => x.CreateStoryAsync(requestStoryDTO, "akash")).ReturnsAsync(dbStatus);
            storiesController.ControllerContext = httpContext;

            //Act
            var actionResult = await storiesController.CreateStory(requestStoryDTO) as BadRequestObjectResult;

            //Assert
            Assert.NotNull(actionResult);
            Assert.Equal(400, actionResult.StatusCode);
            BadResponseDTO badResponseDTO = actionResult.Value as BadResponseDTO;
            Assert.Equal(1, badResponseDTO.Status);
        }



        [Fact]
        public async Task TestCreateStory_Created()
        {

            // Arrange
            Guid expectedStoryId = Guid.NewGuid();
            RequestStoryDTO requestStoryDTO = new RequestStoryDTO
            {
                StoryId = expectedStoryId,
                Title = "",
                Body = "",
                PublishedDate = DateTime.UtcNow
            };

            DBStatus dbStatus = DBStatus.Added;

            storiesService.Setup(x => x.CreateStoryAsync(requestStoryDTO, "akash")).ReturnsAsync(dbStatus);
            storiesController.ControllerContext = httpContext;


            //Act
            var actionResult = await storiesController.CreateStory(requestStoryDTO) as CreatedAtActionResult;

            //Assert
            Assert.NotNull(actionResult);
            Assert.Equal(201, actionResult.StatusCode);
            Assert.Equal(expectedStoryId.ToString(), actionResult.RouteValues.GetValueOrDefault("storyId").ToString());
        }



        [Fact]
        public async Task TestGetStories()
        {
            //Arrange
            string expectedTitle = "LoremIpsum";
            int expectedLength = 1;
            StoriesWithCountDTO storiesWithCountDTO = new StoriesWithCountDTO();
            List<ResponseStoryDTO> storyDTOs = new List<ResponseStoryDTO> { new ResponseStoryDTO { Title = expectedTitle } };
            storiesWithCountDTO.Stories = storyDTOs;
            storiesWithCountDTO.Total = 1;

            storiesService.Setup(x => x.GetStoriesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(storiesWithCountDTO);

            //Act
            var result = await storiesController.GetStories() as OkObjectResult;
            var actualStories = result.Value as StoriesWithCountDTO;

            //Assert
            Assert.NotNull(actualStories);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(expectedLength, actualStories.Total);
            Assert.Equal(storiesWithCountDTO, actualStories);

        }


        [Fact]
        public async Task TestUpdateStory_NotFound()
        {
            //Arrange
            string expectedTitle = "LoremIpsum";
            string userId = "akash";
            DBStatus status = DBStatus.NotFound;
            RequestStoryDTO storyDTO = new RequestStoryDTO { Title = expectedTitle };
            storiesService.Setup(x => x.ReplaceStoryAsync(storyDTO, userId)).ReturnsAsync(status);
            storiesController.ControllerContext = httpContext;

            //Act
            var result = await storiesController.UpdateStory(storyDTO) as NotFoundResult;

            //Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task TestUpdateStory_Ok()
        {
            //Arrange
            string expectedTitle = "LoremIpsum";
            string userId = "akash";
            DBStatus status = DBStatus.Modified;
            RequestStoryDTO storyDTO = new RequestStoryDTO { Title = expectedTitle };
            storiesService.Setup(x => x.ReplaceStoryAsync(storyDTO, userId)).ReturnsAsync(status);
            storiesController.ControllerContext = httpContext;

            //Act
            var result = await storiesController.UpdateStory(storyDTO) as OkObjectResult;

            //Assert
            Assert.NotNull(result);

            var response = result.Value as ResponseStatusDTO;


            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Modified", response.Message);
        }

        [Fact]
        public async Task TestRemoveStory_Forbidden()
        {
            //Arrange
            Guid expectedStoryId = Guid.NewGuid();
            storiesService.Setup(x => x.RemoveStoryAsync(expectedStoryId, "akash")).ReturnsAsync(DBStatus.Forbidden);
            storiesController.ControllerContext = httpContext;

            //Act
            var result = await storiesController.RemoveStory(expectedStoryId) as StatusCodeResult;

            testOutputHelper.WriteLine(result.ToString());

            //Assert
            Assert.NotNull(result);
            Assert.Equal(403, result.StatusCode);
        }
    }
}
