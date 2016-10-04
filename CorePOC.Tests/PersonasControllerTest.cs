using CorePOC.Controllers;
using CorePOC.Models;
using CorePOC.Repositories;
using CorePOC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CorePOC.Tests
{
    public class PersonasControllerTest
    {
        [Fact]
        public void PersonasGet_ReturnListOfPersonas_WhenModelStateValid()
        {
            //Arrange
            var mockRepo = Substitute.For<IPersonaRepository>();
            var controller = new PersonasController(mockRepo);

            //Act
            controller.Get();

            //Assert
            mockRepo.Received().GetAll();
        }

        [Fact]
        public void PersonasPost_ReturnBadRequestResult_WhenModelStateInvalid()
        {
            //Arrange
            var mockRepo = Substitute.For<IPersonaRepository>();
            var controller = new PersonasController(mockRepo);
            controller.ModelState.AddModelError("Testing", "Testing");
            var personavm = new PersonaViewModel();

            //Act
            var result = controller.Post(personavm);

            //Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(badRequestResult.Value, "Error en el modelo");
        }

        [Fact]
        public void PersonasPost_ReturnCreatedtResult_WhenModelStateValid()
        {
            //Arrange
            var mockRepo = Substitute.For<IPersonaRepository>();
            var controller = new PersonasController(mockRepo);
            var personavm = new PersonaViewModel(); //debo pasar valores en props

            //Act
            var result = controller.Post(personavm);

            //Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(createdResult.Value, "Error en el modelo");
        }
    }
}
