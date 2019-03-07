using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -6 )]
	[Parallelizable( ParallelScope.Self )]
	class ThemesMetadataCardConfiguration
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public ThemesMetadataCardConfiguration()
		{
			this.classID = "ThemesMetadataCardConfiguration";
		}

		// Additional assert failure messages.
		private static readonly string ColorMismatch = "Color value was '{0}' when it was expected to be one of '{1}' for the metadatacard theme option '{2}'.";

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Themes Configurations.mfb", users );

			// Declaration of MetadataCard Configuration File Name.
			string configFileName = $"{classID}\\{classID}.json";

			// Configure the declared metadatacard configuration json file in the vault.
			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

		}

		/// <summary>
		/// After all tests have been run, ensure that the browser is closed. Then destroy the 
		/// test environment of the test class.
		/// </summary>
		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}

		/// <summary>
		/// After each test, navigate back to home page if test has passed. But if the test has failed,
		/// then the browser is closed to ensure fresh start for next test.
		/// </summary>
		[TearDown]
		public void EndTest()
		{
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Checks the background color and text color of property group header.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Corporate presentation.pptx",
			"Document",
			"Unclassified Document",
			4,
			"Color of this group title should be MAGENTA and background LIME",
			"rgb(0, 255, 0);rgba(0, 255, 0, 1)",
			"rgb(255, 0, 255);rgba(255, 0, 255, 1)" )]
		public void MetadataCardPropertyGroupHeaderTheme(
			string objectName,
			string objectType,
			string className,
			int expectedPropertyCountAfterSettingClass,
			string groupTitle,
			string expectedBackgroundColor,
			string expectedTextColor )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Change class.
			mdCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Assert that color theme is applied.
			Assert.True( mdCard.ConfigurationOperations.IsColorThemeApplied( MetadataCardThemeConfiguration.GroupHeaderTheme, groupTitle ),
				$"Color theme is not applied for the option '{MetadataCardThemeConfiguration.GroupHeaderTheme.ToString()}' of property group title '{groupTitle}'." );

			// Get the property group header background color.
			string actualColor = mdCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.GroupHeaderTheme, groupTitle );

			// Assert the background color of property group.
			Assert.True( expectedBackgroundColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedBackgroundColor, "Property group header background" ) );

			// Get the property group header text color.
			actualColor = mdCard.ConfigurationOperations.GetTextColor( MetadataCardThemeConfiguration.GroupHeaderTheme, groupTitle );

			// Assert the text color of property group.
			Assert.True( expectedTextColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedTextColor, "Property group header text" ) );

			// Close the metadatacard.
			mdCard.DiscardChanges();
		}

		/// <summary>
		/// Checks the background color, text color, image source and image size of metadata description.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Unclassified Document",
			"Document",
			4,
			"rgb(0, 0, 255);rgba(0, 0, 255, 1)",
			"rgb(255, 255, 255);rgba(255, 255, 255, 1)",
			"http://aframe.com/blog/wp-content/uploads/2015/05/robot-metadata.gif",
			"240px;160px" )]
		public void MetadataCardDescriptionTheme(
			string className,
			string objectType,
			int expectedPropertyCountAfterSettingClass,
			string expectedBackgroundColor,
			string expectedTextColor,
			string expectedDescriptionImageSource,
			string expectedDescriptionImageSize )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Assert that color theme is applied.
			Assert.True( popOutMDCard.ConfigurationOperations.IsColorThemeApplied( MetadataCardThemeConfiguration.DescriptionTheme ),
				$"Color theme is not applied for the option '{MetadataCardThemeConfiguration.DescriptionTheme.ToString()}'." );

			// Get the metadatacard description background color.
			string actualColor = popOutMDCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.DescriptionTheme );

			// Assert the background color of metadatacard description.
			Assert.True( expectedBackgroundColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedBackgroundColor, "Metadatacard description background" ) );

			// Get the metadatacard description text color.
			actualColor = popOutMDCard.ConfigurationOperations.GetTextColor( MetadataCardThemeConfiguration.DescriptionTheme );

			// Assert the text color of metadatacard description.
			Assert.True( expectedTextColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedTextColor, "Metadatacard description text" ) );

			// Assert that expected description image source is set.
			Assert.AreEqual( expectedDescriptionImageSource, popOutMDCard.ConfigurationOperations.MetadataCardDescriptionImageSource,
				"Mismatch between the expected and actual metadata description image source." );

			// Assert that expected description image size is set.
			Assert.AreEqual( expectedDescriptionImageSize, popOutMDCard.ConfigurationOperations.MetadataCardDescriptionImageSize,
				"Mismatch between the expected and actual metadata description image size." );

			// Close the metadatacard.
			popOutMDCard.DiscardChanges();
		}

		/// <summary>
		/// Checks the background color and text color of property description.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Door Chart 01E.dwg",
			"Document",
			"Unclassified Document",
			6,
			"Keywords",
			"rgb(135, 206, 250);rgba(135, 206, 250, 1)",
			"rgb(255, 0, 0);rgba(255, 0, 0, 1)" )]
		public void MetadataCardPropertyDescription(
			string objectName,
			string objectType,
			string className,
			int expectedPropertyCountAfterSettingClass,
			string property,
			string expectedBackgroundColor,
			string expectedTextColor )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Change the class.
			mdCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Assert that color theme is applied.
			Assert.True( mdCard.ConfigurationOperations.IsColorThemeApplied( MetadataCardThemeConfiguration.PropertyDescriptionTheme, property ),
				$"Color theme is not applied for the option '{MetadataCardThemeConfiguration.PropertyDescriptionTheme.ToString()}' of property '{property}'." );

			// Get the property description text color.
			string actualColor = mdCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.PropertyDescriptionTheme, property );

			// Assert the background color of property description.
			Assert.True( expectedBackgroundColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedBackgroundColor, "Property description background" ) );

			// Get the property description text color.
			actualColor = mdCard.ConfigurationOperations.GetTextColor( MetadataCardThemeConfiguration.PropertyDescriptionTheme, property );

			// Assert the text color of property description.
			Assert.True( expectedTextColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedTextColor, "Property description text" ) );

			// Close the metadatacard.
			mdCard.DiscardChanges();
		}

		/// <summary>
		/// Checks the background color and text color of property description.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Unclassified Document",
			4,
			"rgb(255, 0, 0);rgba(255, 0, 0, 1)" )]
		public void MetadataCardAddPropertyLinkColorTheme(
			string className,
			int expectedPropertyCountAfterSettingClass,
			string expectedTextColor )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Making new document object.
			string objectType = "Document";

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Assert that color theme is applied.
			Assert.True( popOutMDCard.ConfigurationOperations.IsColorThemeApplied( MetadataCardThemeConfiguration.AddPropertyTheme ),
				$"Color theme is not applied for the option '{MetadataCardThemeConfiguration.AddPropertyTheme.ToString()}'." );

			// Get the add property link color.
			string actualColor = popOutMDCard.ConfigurationOperations.GetTextColor( MetadataCardThemeConfiguration.AddPropertyTheme );

			// Assert the add property link color.
			Assert.True( expectedTextColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedTextColor, "Add property link text" ) );

			// Close the metadatacard.
			popOutMDCard.DiscardChanges();
		}

		/// <summary>
		/// Checks the footer color theme.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Order - Preliminary Study",
			"Unclassified Document",
			6,
			"rgb(0, 0, 0);rgba(0, 0, 0, 1)",
			"rgb(255, 0, 0);rgba(255, 0, 0, 1)" )]
		public void MetadataCardFooterColorTheme(
			string objectName,
			string className,
			int expectedPropertyCountAfterSettingClass,
			string expectedBackgroundColor,
			string expectedHoverColor )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			// Select the object in search view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Set class.
			mdCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Save the changes in metadatacard.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that color theme is applied.
			Assert.True( mdCard.ConfigurationOperations.IsColorThemeApplied( MetadataCardThemeConfiguration.PermissionFooterTheme ),
				$"Color theme is not applied for the option '{MetadataCardThemeConfiguration.PermissionFooterTheme.ToString()}'." );

			// Get the permission footer background color.
			string actualColor = mdCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.PermissionFooterTheme );

			// Assert the permission footer background color.
			Assert.True( expectedBackgroundColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedBackgroundColor, "Permission Footer" ) );

			// Get the workflow footer background color.
			actualColor = mdCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.WorkflowFooterTheme );

			// Assert the permission footer background color.
			Assert.True( expectedBackgroundColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedBackgroundColor, "Workflow Footer" ) );

			// Get the permission footer hover color.
			actualColor = mdCard.ConfigurationOperations.GetHoverColor( MetadataCardThemeConfiguration.PermissionFooterTheme );

			// Assert the permission footer hover color.
			Assert.True( expectedHoverColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedHoverColor, "Permission Footer- Hover" ) );

			// Get the workflow footer hover color.
			actualColor = mdCard.ConfigurationOperations.GetHoverColor( MetadataCardThemeConfiguration.WorkflowFooterTheme );

			// Assert the permission footer hover color.
			Assert.True( expectedHoverColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedHoverColor, "Workflow Footer - Hover" ) );
		}

		/// <summary>
		/// Check whether the add property link is hidden in the metadatacard.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Document",
			"Other Document",
			5 )]
		public void HiddenAddPropertyLink(
			string objectType,
			string className,
			int expectedPropertyCountAfterSettingClass )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Assert the add property link is not displayed.
			Assert.True( popOutMDCard.ConfigurationOperations.IsAddPropertyLinkHideApplied,
				"Add property link is displayed which is configured to be hidden in the metadatacard." );

			// Close the metadatacard.
			popOutMDCard.DiscardChanges();
		}

		/// <summary>
		/// Checks whether the metadatacard footer is hidden in the metadatacard.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Project Meeting Minutes 2/2006.txt",
			"Document",
			"Other Document",
			10 )]
		public void HiddenMetadataCardFooter(
			string objectName,
			string objectType,
			string className,
			int expectedPropertyCountAfterSettingClass )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view and open the popout metadatacard.
			MetadataCardPopout popOutMDCard = listing.SelectObject( objectName ).PopoutMetadataCard();

			// Set class.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );

			// Assert that metadatacard footer is not displayed.
			Assert.True( popOutMDCard.ConfigurationOperations.IsFooterHideApplied,
				"MetadataCard Footer is displayed which is configured to be hidden in the metadatacard." );

			// Close the metadatacard.
			popOutMDCard.DiscardChanges();
		}

		/// <summary>
		/// Checks whether the related object link is hidden for the related object property in the metadatacard.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Austin District Redevelopment",
			"Project",
			"Customer" )]
		[TestCase(
			"Sales Strategy Development",
			"Project",
			"Project manager" )]
		public void HiddenRelatedObjectLink(
			string objectName,
			string objectType,
			string property )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that related object link of the property is not displayed.
			Assert.True( mdCard.ConfigurationOperations.RelatedObjectLinkHideApplied( property ),
				$"Related object link of the property '{property}' is displayed which is configured to be hidden in the metadatacard." );
		}

		/// <summary>
		/// Checks whether the object id field is hidden in the metadatacard header.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Nancy Hartwick",
			"Contact person",
			"Customer" )]
		public void HiddenObjectIDField(
			string objectName,
			string objectType,
			string property )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that object id field is not displayed.
			Assert.True( mdCard.ConfigurationOperations.IsObjIDFieldHideApplied,
				"Object id field is displayed which is configured to be hidden in the metadatacard." );

			// Assert that object version field is displayed.
			Assert.False( mdCard.ConfigurationOperations.IsObjVersionFieldHideApplied,
				"Object version field is not displayed which is not configured to be hidden in the metadatacard." );
		}

		/// <summary>
		/// Checks whether the object version field is hidden in the metadatacard header.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Andy Nash",
			"Employee",
			"Customer" )]
		public void HiddenObjectVersionAndIDFields(
			string objectName,
			string objectType,
			string property )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that object version field is not displayed.
			Assert.True( mdCard.ConfigurationOperations.IsObjVersionFieldHideApplied,
				"Object version field is displayed which is configured to be hidden in the metadatacard." );

			// Assert that object id field is not displayed.
			Assert.True( mdCard.ConfigurationOperations.IsObjIDFieldHideApplied,
				"Object id field is displayed which is configured to be hidden in the metadatacard." );
		}

		/// <summary>
		/// Checks whether the supported color code formats works in the metadatacard.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemesConfiguration" )]
		[TestCase(
			"New Assignment",
			"Assignment",
			"rgb(250, 88, 172);rgba(250, 88, 172, 1)",
			"rgb(255, 255, 0);rgba(255, 255, 0, 1)" )]
		public void MetadataCardButtonsColorTheme(
			string objectName,
			string objectType,
			string expectedButtonColor,
			string expectedButtonHoverColor )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open the metadata card of the new object.
			MetadataCardPopout popOutMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the actual button color.
			string actualColor = popOutMDCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.ButtonTheme );

			// Assert that expected button color is set in the metadatacard.
			Assert.True( expectedButtonColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedButtonColor, "Buttons" ) );

			// Get the actual button color when hover the button.
			actualColor = popOutMDCard.ConfigurationOperations.GetHoverColor( MetadataCardThemeConfiguration.ButtonTheme );

			// Assert that expected button color is set in the metadatacard.
			Assert.True( expectedButtonHoverColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedButtonHoverColor, "Buttons - Hover" ) );

			// Close the metadatacard.
			popOutMDCard.DiscardChanges();
		}

		/// <summary>
		/// Open metadata card of an object and verify that the option ribbon displays configured color.
		/// Then modify some property to activate edit mode, and verify that the option ribbon displays
		/// another configured color on edit mode. Finally, save changes and verify that color changes
		/// back to the original configured color.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Fortney Nolte Associates",
			"Customer",
			"rgb(250, 88, 172);rgba(250, 88, 172, 1)",
			"rgb(255, 0, 0);rgba(255, 0, 0, 1)",
			"Description",
			"To activate edit mode" )]
		public void MetadataCardRibbonColorTheme(
			string objectName,
			string objectType,
			string expectedRibbonColor,
			string expectedRibbonColorInEditMode,
			string property,
			string value )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardPopout popOutMDCard = listing.SelectObject( objectName ).PopoutMetadataCard();

			// Assert that color theme is applied.
			Assert.True( popOutMDCard.ConfigurationOperations.IsColorThemeApplied( MetadataCardThemeConfiguration.RibbonTheme ),
				$"Color theme is not applied for the option '{MetadataCardThemeConfiguration.RibbonTheme.ToString()}'." );

			// Get the actual ribbon color.
			string actualColor = popOutMDCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.RibbonTheme );

			// Assert that expected color is set for the metadatacard ribbon.
			Assert.True( expectedRibbonColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedRibbonColor, "Ribbon" ) );

			// Modify a property value to activate edit mode in metadata card.
			popOutMDCard.Properties.AddPropertyAndSetValue( property, value );

			// Get the actual ribbon color in edit mode.
			actualColor = popOutMDCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.RibbonTheme );

			// Assert that expected color is set for the metadatacard ribbon.
			Assert.True( expectedRibbonColorInEditMode.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedRibbonColorInEditMode, "Ribbon (In Edit mode)" ) );

			// Close the metadatacard.
			popOutMDCard.DiscardChanges();
		}

		/// <summary>
		/// Checks whether the invalid color code formats not applied in the metadatacard.
		/// </summary>
		[Test]
		[Category( "MetadataCardThemes" )]
		[TestCase(
			"Training Slides",
			"Document collection",
			"rgb(230, 230, 230);rgba(230, 230, 230, 1)",
			"rgb(239, 250, 255);rgba(239, 250, 255, 1)",
			"rgb(153, 153, 153);rgba(153, 153, 153, 1)" )]
		public void InvalidColorCodeFormats(
			string objectName,
			string objectType,
			string expectedRibbonColor,
			string expectedDescriptionBackgroundColor,
			string expectedDescriptionTextColor )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform search to select the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the ribbon color.
			string actualColor = mdCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.RibbonTheme );

			// Assert that default color code is applied.
			Assert.True( expectedRibbonColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedRibbonColor, "Ribbon" ) );

			// Get the description background color.
			actualColor = mdCard.ConfigurationOperations.GetBackgroundColor( MetadataCardThemeConfiguration.DescriptionTheme );

			// Assert that default color code is applied.
			Assert.True( expectedDescriptionBackgroundColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedDescriptionBackgroundColor, "Description - Background" ) );

			// Get the description text color.
			actualColor = mdCard.ConfigurationOperations.GetTextColor( MetadataCardThemeConfiguration.DescriptionTheme );

			// Assert that default color code is applied.
			Assert.True( expectedDescriptionTextColor.Contains( actualColor ),
				string.Format( ColorMismatch, actualColor, expectedDescriptionTextColor, "Description - Text" ) );
		}
	}
}
