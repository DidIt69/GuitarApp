//using Xunit;
//using GuitarApp.Core.Services;

//namespace GuitarApp.UnitTests;

//public class TuningEvaluatorTests
//{
//    [Fact]
//    public async Task EvaluatePitch_WithPerfectA2String_ShouldReturnScore100()
//    {
//        // Arrange : On simule une corde de LA parfaite à 110 Hz
//        var evaluator = new TuningEvaluator();

//        // Act
//        var result = evaluator.EvaluatePitch(110.0);

//        // Assert
//        Assert.Equal("La (2)", result.NoteName);
//        Assert.Equal(0, result.CentsError);
//        Assert.Equal(100, result.AccuracyScore);
//        Assert.True(result.IsInTune);
//    }

//    [Fact]
//    public async Task EvaluatePitch_WithSlightlySharpE4String_ShouldCalculateCorrectCentsAndScore()
//    {
//        // Arrange : Mi aigu théorique = 329.63 Hz. On simule un jeu trop haut (Sharp) à 331.0 Hz
//        var evaluator = new TuningEvaluator();

//        // Act
//        var result = evaluator.EvaluatePitch(331.0);

//        // Assert
//        Assert.Equal("Mi (4)", result.NoteName);
//        Assert.True(result.CentsError > 0, "L'erreur en cents doit être positive (note trop haute).");
//        Assert.True(result.AccuracyScore < 100, "Le score de précision doit être pénalisé.");
//        Assert.False(result.IsInTune, "La note est trop loin de la cible pour être considérée comme juste.");
//    }
//}
