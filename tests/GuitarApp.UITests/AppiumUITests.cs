//using NUnit.Framework;
//using OpenQA.Selenium;
//using OpenQA.Selenium.Appium;
//using OpenQA.Selenium.Appium.Windows;
//using System;

//namespace GuitarApp.UITests;

//[TestFixture]
//public class AppiumUiTests
//{
//    private WindowsDriver<WindowsElement>? _driver;
//    private const string AppiumServerUrl = "http://localhost:4723";

//    // ID de l'application Windows généré par MAUI (à adapter selon votre AppXManifest)
//    private const string AppId = "GuitarApp_1.0.0.0_x64__neutral_h855g2db8788g!App";

//    [SetUp]
//    public void OpenApplication()
//    {
//        var options = new AppiumOptions();
//        options.AddAdditionalCapability("app", AppId);
//        options.AddAdditionalCapability("deviceName", "WindowsPC");
//        options.AddAdditionalCapability("platformName", "Windows");

//        try
//        {
//            // Initialisation du pilote Appium pour Windows
//            _driver = new WindowsDriver<WindowsElement>(new Uri(AppiumServerUrl), options);
//            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
//        }
//        catch (Exception ex)
//        {
//            Assert.Inconclusive($"Impossible de démarrer le serveur Appium sur {AppiumServerUrl}. Vérifiez qu'Appium est lancé. Erreur : {ex.Message}");
//        }
//    }

//    /// <summary>
//    /// TEST DE NAVIGATION : Vérifie que le clic sur le bouton de l'écran principal
//    /// ouvre correctement la page d'historique et affiche le bon message d'état vide.
//    /// </summary>
//    [Test]
//    public void NavigateToHistory_ShouldDisplayEmptyStateMessage()
//    {
//        if (_driver == null) Assert.Ignore("Le driver Appium n'est pas initialisé.");

//        // 1. Recherche du bouton sur MainView via son AutomationId (AccessibilityId dans Appium)
//        WindowsElement btnHistory = _driver.FindElementByAccessibilityId("BtnGoToHistory");
//        Assert.That(btnHistory.Displayed, Is.True, "Le bouton de navigation vers l'historique n'est pas visible.");

//        // 2. Action : Clic de l'utilisateur sur le bouton
//        btnHistory.Click();

//        // 3. Attente et vérification : On cherche le label d'état vide sur la page HistoryView
//        WindowsElement lblEmptyState = _driver.FindElementByAccessibilityId("LblEmptyHistoryState");

//        // 4. Assertion : L'écran a bien changé et le texte de l'EmptyView est correct
//        Assert.That(lblEmptyState.Displayed, Is.True, "La page d'historique ne s'est pas affichée.");
//        Assert.That(lblEmptyState.Text, Contains.Substring("Aucune session enregistrée"),
//            "Le texte de l'état vide de l'historique est incorrect.");
//    }

//    [TearDown]
//    public void CloseApplication()
//    {
//        // Nettoyage et fermeture de l'application après le test
//        if (_driver != null)
//        {
//            _driver.Quit();
//            _driver.Dispose();
//        }
//    }
//}
