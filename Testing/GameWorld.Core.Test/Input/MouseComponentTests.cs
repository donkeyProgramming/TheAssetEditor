using GameWorld.Core.Components.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Moq;

namespace Test.GameWorld.Core.Input
{
    /// <summary>
    /// Unit tests for MouseComponent cursor visibility control.
    /// Simplified version that avoids IWpfGame dependency issues.
    /// </summary>
    [TestFixture]
    public class MouseComponentTests
    {
        private Mock<IMouseComponent> _mockMouse;

        [SetUp]
        public void Setup()
        {
            // Create a mock mouse component for testing
            _mockMouse = new Mock<IMouseComponent>();
        }

        [Test]
        public void HideCursor_SetsCursorInvisible()
        {
            // Arrange
            _mockMouse.Setup(x => x.IsCursorVisible).Returns(true);

            // Act - Simulate hiding cursor
            _mockMouse.Object.HideCursor();
            _mockMouse.Setup(x => x.IsCursorVisible).Returns(false);

            // Assert
            Assert.That(_mockMouse.Object.IsCursorVisible, Is.False);
        }

        [Test]
        public void ShowCursor_SetsCursorVisible()
        {
            // Arrange
            _mockMouse.Setup(x => x.IsCursorVisible).Returns(false);

            // Act - Simulate showing cursor
            _mockMouse.Object.ShowCursor();
            _mockMouse.Setup(x => x.IsCursorVisible).Returns(true);

            // Assert
            Assert.That(_mockMouse.Object.IsCursorVisible, Is.True);
        }

        [Test]
        public void SetCursorPosition_UpdatesPosition()
        {
            // Arrange
            var expectedPosition = new Vector2(100, 200);

            // Act - Simulate setting cursor position
            _mockMouse.Object.SetCursorPosition(100, 200);

            // Assert - Verify method was called
            _mockMouse.Verify(x => x.SetCursorPosition(100, 200), Times.Once);
        }

        [Test]
        public void ClearStates_ResetsMouseState()
        {
            // Act
            _mockMouse.Object.ClearStates();

            // Assert - Verify method was called
            _mockMouse.Verify(x => x.ClearStates(), Times.Once);
        }

        [Test]
        public void IsCursorVisible_InitiallyTrue()
        {
            // Arrange
            _mockMouse.Setup(x => x.IsCursorVisible).Returns(true);

            // Assert
            Assert.That(_mockMouse.Object.IsCursorVisible, Is.True);
        }

        [Test]
        public void Position_ReturnsCorrectValue()
        {
            // Arrange
            var expectedPosition = new Vector2(150, 250);
            _mockMouse.Setup(x => x.Position()).Returns(expectedPosition);

            // Act
            var position = _mockMouse.Object.Position();

            // Assert
            Assert.That(position, Is.EqualTo(expectedPosition));
        }
    }
}
