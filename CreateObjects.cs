using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.AssertHelpers;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -15 )]
	[Parallelizable( ParallelScope.Self )]
	class CreateObjects
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

		public CreateObjects()
		{
			this.classID = "CreateObjects";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Data Types And Test Objects.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );
		}

		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( mfContext );
		}

		[TearDown]
		public void EndTest()
		{
			// Returns the browser to the home page to be used by the next test or quits the browser if
			// the test failed.
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}


		/// <summary>
		/// Asserts that newly created object is checked in and has correct time stamps and user information in listing 
		/// and metadata card header.
		/// </summary>
		/// <param name="objectName">Object's name as displayed in listing. </param>
		/// <param name="objectType">Object type.</param>
		/// <param name="timeAtObjCreation">Timestamp at object creation.</param>
		/// <param name="header">Metadata card header page object of the created object.</param>
		/// <param name="listing">ListView page object.</param>
		private void AssertCheckedInNewObject( string objectName, string objectType, string timeAtObjCreation,
			HeaderInMetadataCard header, ListView listing )
		{
			// Assert that the newly created object appears to listing and that it is instantly checked in.
			Assert.True( listing.IsItemInListing( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );

			// Assert that metadata card displays the object as checked in.
			Assert.AreEqual( CheckOutStatus.CheckedIn, header.CheckOutStatus );

			string expectedUser = this.mfContext.UsernameOfUser( "user" );

			MetadataCardAssertHelper.AssertNewObjectMDCardHeaderInfo( objectType, expectedUser, timeAtObjCreation, header );

			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( 1, expectedUser, timeAtObjCreation, header );

		}


		/// <summary>
		/// Asserts that newly created object is checked out and has correct time stamps and user information in listing 
		/// and metadata card header.
		/// </summary>
		/// <param name="objectName">Object's name as displayed in listing. </param>
		/// <param name="objectType">Object type.</param>
		/// <param name="timeAtObjCreation">Timestamp at object creation.</param>
		/// <param name="header">Metadata card header page object of the created object.</param>
		/// <param name="listing">ListView page object.</param>
		private void AssertCheckedOutNewObject( string objectName, string objectType, string timeAtObjCreation,
			HeaderInMetadataCard header, ListView listing )
		{
			// Assert that the newly created object appears to listing and that it is instantly checked out.
			Assert.True( listing.IsItemInListing( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, listing.GetObjectCheckOutStatus( objectName ) );

			// Assert that metadata card displays the object as checked out.
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, header.CheckOutStatus );

			string expectedUser = this.mfContext.UsernameOfUser( "user" );

			MetadataCardAssertHelper.AssertNewObjectMDCardHeaderInfo( objectType, expectedUser, timeAtObjCreation, header );

			MetadataCardAssertHelper.AssertCheckedOutObjectMDCardHeaderInfo( 1, expectedUser, timeAtObjCreation,
				expectedUser, timeAtObjCreation, header );
		}

		/// <summary>
		/// Create object by simply filling its name and some other property that may be required or some other property.
		/// This tests only object types that don't have any templates so the template dialog is skipped. Verifies that
		/// the object is checked in by default, version and object type are displayed, and that timestamps and user
		/// information is correct after object creation.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Customer",
			"Customer name",
			"Created customer",
			"City",
			"Helsinki",
			Description = "Non-document object type" )]
		[TestCase(
			"Child object",
			"Name or title",
			"Created child",
			"Owner (Parent object)",
			"Test parent",
			Description = "Sub-object." )]
		[TestCase(
			"Assignment",
			"Name or title",
			"Created assignment",
			"Assigned to",
			"Alex Kramer",
			Description = "Assignment object." )]
		public void NewCheckedInObjectListingAndMDCardHeaderBasics(
			string objectType,
			string nameProperty,
			string objectName,
			string property,
			string value )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search with object name and set object type filter. Note that at this point the object is not yet created.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Assert that any object with exact matching name doesn't already exist in listing.
			Assert.False( listing.IsItemInListing( objectName ),
				"Object with name '" + objectName + "' already exists in listing before creating it." );

			// Start creating new object.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Set object name.
			newObjMDCard.Properties.SetPropertyValue( nameProperty, objectName );

			// Set another property.
			newObjMDCard.Properties.SetPropertyValue( property, value );

			string timeAtObjCreation = TimeHelper.GetCurrentTime();

			// Click create button and the metadata card of the new object appears to the right pane.
			MetadataCardRightPane mdCard = newObjMDCard.SaveAndDiscardOperations.Save();

			// Assert that object is in checked in state and metadata card displays correct information.
			this.AssertCheckedInNewObject( objectName, objectType, timeAtObjCreation, mdCard.Header, listing );
			Assert.AreEqual( objectName, mdCard.Properties.GetPropertyValue( nameProperty ) );
			Assert.AreEqual( value, mdCard.Properties.GetPropertyValue( property ) );

		}

		/// <summary>
		/// Create object by unchecking the "Check in immediately" checkbox and by simply filling its name and some other property 
		/// that may be required or some other property. This tests only object types that don't have any templates so the template 
		/// dialog is skipped. Verifies that the object is created as checked out object, version and object type are displayed, 
		/// and that timestamps and user information is correct after object creation.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Customer",
			"Customer name",
			"Checkout customer",
			"Country",
			"United Kingdom",
			Description = "Non-document object type" )]
		[TestCase(
			"Child object",
			"Name or title",
			"Checkout child",
			"Owner (Parent object)",
			"Test parent",
			Description = "Sub-object." )]
		[TestCase(
			"Assignment",
			"Name or title",
			"Checkout assignment",
			"Assigned to",
			"John Stewart",
			Description = "Assignment object." )]
		public void NewCheckedOutObjectListingAndMDCardHeaderBasics(
			string objectType,
			string nameProperty,
			string objectName,
			string property,
			string value )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search with object name and set object type filter. Note that at this point the object is not yet created.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Assert that any object with extact matching name doesn't already exist in listing.
			Assert.False( listing.IsItemInListing( objectName ),
				"Object with name '" + objectName + "' already exists in listing before creating it." );

			// Start creating new object.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Set object name and some other property.
			newObjMDCard.Properties.SetPropertyValue( nameProperty, objectName );
			newObjMDCard.Properties.SetPropertyValue( property, value );

			// Set the object to not be checked in. By default that is selected so the click unchecks the checkbox.
			newObjMDCard.CheckInImmediatelyClick();

			string timeAtObjCreation = TimeHelper.GetCurrentTime();

			// Click create button and the metadata card of the new object appears to the right pane.
			MetadataCardRightPane mdCard = newObjMDCard.SaveAndDiscardOperations.Save();

			// Assert that object is in checked out state and metadata card displays correct infromation.
			this.AssertCheckedOutNewObject( objectName, objectType, timeAtObjCreation, mdCard.Header, listing );
			Assert.AreEqual( objectName, mdCard.Properties.GetPropertyValue( nameProperty ) );
			Assert.AreEqual( value, mdCard.Properties.GetPropertyValue( property ) );

			// Check in the newly created object.
			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );
			mdCard = contextMenu.CheckInObject();

			// Assert that object is in checked in state and metadata card displays correct information.
			this.AssertCheckedInNewObject( objectName, objectType, timeAtObjCreation, mdCard.Header, listing );
			Assert.AreEqual( objectName, mdCard.Properties.GetPropertyValue( nameProperty ) );
			Assert.AreEqual( value, mdCard.Properties.GetPropertyValue( property ) );

		}

		/// <summary>
		/// Create object by unchecking the "check in immediately" checkbox. This tests only object types that don't have any 
		/// templates so the template dialog is skipped. Then making undo checkout operation which removes the object because
		/// it was never checked in yet.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Customer",
			"Customer name",
			"UndoCheckout customer",
			"Country",
			"United Kingdom",
			Description = "Non-document object type" )]
		[TestCase(
			"Child object",
			"Name or title",
			"UndoCheckout child",
			"Owner (Parent object)",
			"Test parent",
			Description = "Sub-object." )]
		[TestCase(
			"Assignment",
			"Name or title",
			"UndoCheckout assignment",
			"Assigned to",
			"John Stewart",
			Description = "Assignment object." )]
		public void UndoCheckoutNewCheckedOutObject(
			string objectType,
			string nameProperty,
			string objectName,
			string property,
			string value )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search with object name and set object type filter. Note that at this point the object is not yet created.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Assert that any object with extact matching name doesn't already exist in listing.
			Assert.False( listing.IsItemInListing( objectName ),
				"Object with name '" + objectName + "' already exists in listing before creating it." );

			// Start creating new object.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Set object name and some other property.
			newObjMDCard.Properties.SetPropertyValue( nameProperty, objectName );
			newObjMDCard.Properties.SetPropertyValue( property, value );

			// Set the object to not be checked in. By default that is selected so the click unchecks the checkbox.
			newObjMDCard.CheckInImmediatelyClick();

			newObjMDCard.SaveAndDiscardOperations.Save();

			// Created object should be in listing.
			Assert.True( listing.IsItemInListing( objectName ) );

			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );

			// Undo checkout will make the object disappear because it was never checked in yet and thus no
			// versions exist in the server.
			listing = contextMenu.UndoCheckOutObjectClearsSelection();

			// After undo checkout, the created object should no longer exist in listing.
			Assert.False( listing.IsItemInListing( objectName ) );

			homePage.SearchPane.QuickSearch( objectName );

			// The object should not be found by search either after undo checkout.
			Assert.False( listing.IsItemInListing( objectName ) );
		}


		/// <summary>
		/// Create document by selecting a blank template. Test fills document's class and name in the metadata card.
		/// Also selects to check in immediately which is not selected by default for document objects.
		/// Verifies that the document is checked in, version and object type are displayed, and that timestamps and user
		/// information is correct after object creation.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			".bmp",
			"Bitmap Image (.bmp)",
			"Picture",
			"Test picture",
			6 )]
		[TestCase(
			".xlsx",
			"Microsoft Excel Worksheet (.xlsx)",
			"Price List",
			"Test prices",
			5 )]
		[TestCase(
			".pptx",
			"Microsoft PowerPoint Presentation (.pptx)",
			"Agenda",
			"Test agenda presentation",
			8 )]
		[TestCase(
			".docx",
			"Microsoft Word Document (.docx)",
			"Document",
			"Test word document",
			2 )]
		[TestCase(
			".txt",
			"Text Document (.txt)",
			"Memo",
			"Test text memo",
			8 )]
		[TestCase(
			"",
			"Multi-File Document",
			"Other Marketing Material",
			"Test mfd",
			5 )]
		public void NewCheckedInDocumentFromBlankTemplateListingAndMDCardHeaderBasics(
			string fileType,
			string template,
			string className,
			string objectTitle,
			int expectedPropertyCountAfterSettingClass )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectTitle );

			// Making new document object.
			string objectType = "Document";

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );
			templateSelector.SelectTemplate( template );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class and name.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );
			popOutMDCard.Properties.SetPropertyValue( "Name or title", objectTitle );

			// For documents "Open for editing" is selected by default. Use checkbox to "Check in immediately" instead.
			popOutMDCard.CheckInImmediatelyClick();

			string timeAtObjCreation = TimeHelper.GetCurrentTime();

			// Save. The metadata card opens to right pane.
			MetadataCardRightPane mdCard = popOutMDCard.SaveAndDiscardOperations.Save();

			// Object name is the combination of title and file type.
			string objectName = objectTitle + fileType;

			// Assert that the object is checked in and it has correct information displayed in metadata card.
			this.AssertCheckedInNewObject( objectName, objectType, timeAtObjCreation, mdCard.Header, listing );
			Assert.AreEqual( className, mdCard.Properties.GetPropertyValue( "Class" ) );
			Assert.AreEqual( objectTitle, mdCard.Properties.GetPropertyValue( "Name or title" ) );

		}

		/// <summary>
		/// Create document by selecting a blank template. Test fills document's class and name in the metadata card.
		/// Also clicks the open for editing checkbox: For normal documents this causes that no checkboxes are selected, so the 
		/// object will be checked out. For mfd objects this will actually check the open for editing checkbox but it only
		/// checks out the mfd, so it is ok. Verifies that the document is checked out, version and object type are displayed,
		/// and that timestamps and user information is correct after object creation.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			".docx",
			"Microsoft Word Document (.docx)",
			"Statement",
			"Test checkout document",
			6 )]
		[TestCase(
			"",
			"Multi-File Document",
			"Other Document",
			"Test checkout mfd",
			4 )]
		public void NewCheckedOutDocumentFromBlankTemplateListingAndMDCardHeaderBasics(
			string fileType,
			string template,
			string className,
			string objectTitle,
			int expectedPropertyCountAfterSettingClass )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectTitle );

			// Making new document object.
			string objectType = "Document";

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );
			templateSelector.SelectTemplate( template );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class and name.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );
			popOutMDCard.Properties.SetPropertyValue( "Name or title", objectTitle );

			// For documents "Open for editing" is selected by default. Click check box to uncheck the
			// option so nothing is selected. This means that the document will be checked out.
			popOutMDCard.OpenForEditingClick();

			string timeAtObjCreation = TimeHelper.GetCurrentTime();

			// Save. The metadata card opens to right pane.
			MetadataCardRightPane mdCard = popOutMDCard.SaveAndDiscardOperations.Save();

			// Object name is the combination of title and file type.
			string objectName = objectTitle + fileType;

			// Assert that the object is checked out and it has correct information displayed in metadata card.
			this.AssertCheckedOutNewObject( objectName, objectType, timeAtObjCreation, mdCard.Header, listing );
			Assert.AreEqual( className, mdCard.Properties.GetPropertyValue( "Class" ) );
			Assert.AreEqual( objectTitle, mdCard.Properties.GetPropertyValue( "Name or title" ) );

			// Check in the newly created object.
			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );
			mdCard = contextMenu.CheckInObject();

			// Assert that object is in checked in state and metadata card displays correct information.
			this.AssertCheckedInNewObject( objectName, objectType, timeAtObjCreation, mdCard.Header, listing );
			Assert.AreEqual( className, mdCard.Properties.GetPropertyValue( "Class" ) );
			Assert.AreEqual( objectTitle, mdCard.Properties.GetPropertyValue( "Name or title" ) );

		}

		/// <summary>
		/// Create document by unchecking the "open for editing" checkbox which causes nothing to be selected and the
		/// object will be checked out. For mfd objects this will actually check the open for editing checkbox but 
		/// it also just checks out the mfd, so it is ok. Then making undo checkout operation which removes the object 
		/// because it was never checked in yet.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			".docx",
			"Microsoft Word Document (.docx)",
			"Message or Letter",
			"Test undocheckout document",
			7 )]
		[TestCase(
			"",
			"Multi-File Document",
			"Bulletin or Press Release",
			"Test undocheckout mfd",
			6 )]
		public void UndoCheckoutNewCheckedOutDocument(
			string fileType,
			string template,
			string className,
			string objectTitle,
			int expectedPropertyCountAfterSettingClass )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectTitle );

			// Making new document object.
			string objectType = "Document";

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );
			templateSelector.SelectTemplate( template );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class and name.
			popOutMDCard.Properties.SetPropertyValue( "Class", className, expectedPropertyCountAfterSettingClass );
			popOutMDCard.Properties.SetPropertyValue( "Name or title", objectTitle );

			// For documents "Open for editing" is selected by default. Click check box to uncheck the
			// option so nothing is selected. This means that the document will be checked out.
			popOutMDCard.OpenForEditingClick();

			// Save. The metadata card opens to right pane.
			MetadataCardRightPane mdCard = popOutMDCard.SaveAndDiscardOperations.Save();

			string objectName = objectTitle + fileType;

			// Created object should be in listing.
			Assert.True( listing.IsItemInListing( objectName ) );

			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );

			// Undo checkout will make the object disappear because it was never checked in yet and thus no
			// versions exist in the server.
			listing = contextMenu.UndoCheckOutObjectClearsSelection();

			// After undo checkout, the created object should no longer exist in listing.
			Assert.False( listing.IsItemInListing( objectName ) );

			homePage.SearchPane.QuickSearch( objectName );

			// The object should not be found by search either after undo checkout.
			Assert.False( listing.IsItemInListing( objectName ) );
		}

		/// <summary>
		/// Create new document from template and check in the document immediately. Checks that 
		/// property value is copied from template to the created document.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"AutoCAD Drawing Template (ISO A1).dwg",
			"Drawing from template",
			"Drawing",
			"Revision number",
			"0.1" )]
		public void NewDocumentFromTemplate(
			string templateName,
			string objectTitle,
			string className,
			string property,
			string value )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Document";

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectTitle, objectType );

			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.All );
			templateSelector.SelectTemplate( templateName );

			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			string expectedText = "From template: " + templateName;

			// Assert that template info is displayed correctly.
			Assert.AreEqual( expectedText, popOutMDCard.TemplateInfo );

			// Assert that class and a property from template is already filled.
			Assert.AreEqual( className, popOutMDCard.Properties.GetPropertyValue( "Class" ) );
			Assert.AreEqual( value, popOutMDCard.Properties.GetPropertyValue( property ) );

			// Assert that Is template property is not shown to object created from template.
			Assert.False( popOutMDCard.Properties.IsPropertyInMetadataCard( "Is template" ) );

			popOutMDCard.Properties.SetPropertyValue( "Name or title", objectTitle );

			popOutMDCard.CheckInImmediatelyClick();

			MetadataCardRightPane mdCard = popOutMDCard.SaveAndDiscardOperations.Save();

			// Assert name, class, and property after creating object.
			Assert.AreEqual( objectTitle, mdCard.Properties.GetPropertyValue( "Name or title" ) );
			Assert.AreEqual( className, mdCard.Properties.GetPropertyValue( "Class" ) );
			Assert.AreEqual( value, mdCard.Properties.GetPropertyValue( property ) );

			// Assert that Is template property is not shown to object created from template.
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( "Is template" ) );
		}

		/// <summary>
		/// Create new document from template by first selecting class and then a class specific template.
		/// The new document is checked in immediately.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Test doc from class template",
			"Other Document",
			"Document Template for Word.doc" )]
		public void NewDocumentFromClassSpecificTemplate(
			string objectTitle,
			string className,
			string templateName )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Document";

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectTitle, objectType );

			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			templateSelector.SelectClass( className );

			templateSelector.SelectTemplate( templateName );

			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			string expectedText = "From template: " + templateName;

			// Assert that the object is being created from the selected template and that
			// it has the expected class.
			Assert.AreEqual( expectedText, popOutMDCard.TemplateInfo );
			Assert.AreEqual( className, popOutMDCard.Properties.GetPropertyValue( "Class" ) );

			popOutMDCard.Properties.SetPropertyValue( "Name or title", objectTitle );

			popOutMDCard.CheckInImmediatelyClick();

			MetadataCardRightPane mdCard = popOutMDCard.SaveAndDiscardOperations.Save();

			// Assert that object class and name are saved after object creation.
			Assert.AreEqual( objectTitle, mdCard.Properties.GetPropertyValue( "Name or title" ) );
			Assert.AreEqual( className, mdCard.Properties.GetPropertyValue( "Class" ) );
		}

		/// <summary>
		/// Update the existing object with 'Is template' as true and
		/// Check whether template selector has that item.
		/// </summary>
		[Test]
		[Category( "Templates" )]
		[TestCase(
			"",
			"MFD Template",
			"Multi-File Document" )]
		public void NewDocumentTemplateObject(
			string extension,
			string newTemplateName,
			string templateName )
		{
			// Starts the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Object type variable declaration.
			string objectType = "Document";

			// Open the document object template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Set the template filter.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.All );

			// Select the template.
			templateSelector.SelectTemplate( templateName );

			// Navigate to the metadatacard after selecting the template.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set the name property value.
			popOutMDCard.Properties.SetPropertyValue( "Class", "Other Document", 4 );
			popOutMDCard.Properties.SetPropertyValue( "Name or title", newTemplateName );

			// Add the 'Is template' property with value 'Yes'.
			popOutMDCard.Properties.AddProperty( "Is template" );
			popOutMDCard.Properties.SetCheckBoxTypeBooleanPropertyValue( "Is template", true );

			// Set check-in immediately option.
			popOutMDCard.CheckInImmediatelyClick();

			// Create the template object.
			popOutMDCard.SaveAndDiscardOperations.Save( false );

			// Open the document object template selector.
			templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Set the template filter.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.RecentlyUsed );

			// Assert that the template which is used to create the new template is listed in the recently used filter.
			Assert.True( templateSelector.IsTemplateInDialog( templateName ), "Recently used template '" + templateName + "' is not listed under the recently used filter in template selector." );

			// Set the template filter.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.All );

			// Assert that the new template is displayed in the template selector.
			Assert.True( templateSelector.IsTemplateInDialog( newTemplateName + extension ), "Newly added template '" + newTemplateName + extension + "' is not exists in the template selector." );

			// Close the template selector.
			templateSelector.CancelButtonClick();

		}

		/// <summary>
		/// Update the existing object with 'Is template' as true and
		/// Check whether template selector has that item.
		/// </summary>
		[Test]
		[Category( "Templates" )]
		[TestCase( "Project Meeting Minutes 2/2006.txt" )]
		public void NewDocumentTemplateFromExistingObject( string templateObject )
		{
			// Starts the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Object type variable declaration.
			string objectType = "Document";

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( templateObject, objectType );

			// Select the object in the list view.
			MetadataCardRightPane mdCard = listing.SelectObject( templateObject );

			// Add the 'Is template' property with value 'Yes'.
			mdCard.Properties.AddProperty( "Is template" );
			mdCard.Properties.SetCheckBoxTypeBooleanPropertyValue( "Is template", true );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save();

			// Open the new document object metadatacard.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Set the template filter.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.All );

			// Assert that the new template is displayed in the template selector.
			Assert.True( templateSelector.IsTemplateInDialog( templateObject ),
				"Newly added template '" + templateObject + "' doesn't exists in the template selector." );

			// Close the template selector.
			templateSelector.CancelButtonClick();

		}
	}
}