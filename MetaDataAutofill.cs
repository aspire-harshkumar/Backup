using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using NUnit.Framework;
using NUnit.Framework.Internal;


namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -6 )]
	[Parallelizable( ParallelScope.Self )]
	class MetadataAutofill
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		/// <summary>
		/// Additional assert messages to be printed with the assert methods.
		/// </summary>
		private static readonly string SetPropertyValueMismatchMessage = "Mismatch between the expected and actual set property '{0}' value in the metadatacard.";
		private static readonly string AutosetPropertyMismatchMessage = "Mismatch between the expected and actual autoset property '{0}' value in the metadatacard.";
		private static readonly string AutofillPropertyMismatchMessage = "Mismatch between the expected and actual autofilled property '{0}' value in the metadatacard.";
		private static readonly string AutofillDialogMessage = "You selected " + "\"{0}\"" + " ({1}) for a property value.\r\n\r\n Do you want to auto-fill the following metadata fields based on the selected property value?\r\n \r\n{2} - {3}";

		/// <summary>
		/// This assert messages to be printed with the assert methods when multiple autofilled value is populated in autofill dialog
		/// </summary>
		private static readonly string AutofillPropertiesDialogMessage = "You selected " + "\"{0}\"" + " ({1}) for a property value.\r\n\r\n Do you want to auto-fill the following metadata fields based on the selected property value?\r\n \r\n{2} - {3}; {4}";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		/// <summary>
		/// Formats autofill message for Edge browser. In Edge, the message
		/// has less space characters.
		/// </summary>
		/// <param name="autofillMessage">Autofill message.</param>
		/// <returns>Autofill message for Edge.</returns>
		private string FormatAutofillMessageForEdge( string autofillMessage )
		{
			// Format the autofill message for Edge browser. Remove space characters
			// after each line break.
			return autofillMessage.Replace( "\r\n ", "\r\n" );
		}

		/// <summary>
		/// Get allowed autofill dialog messages as an array of strings.
		/// The messages may be slightly different for some browsers, for example
		/// less space characters after line breaks, and therefore the verification
		/// should accept these different formats.
		/// </summary>
		/// <param name="manualPropValue">Value that the users sets.</param>
		/// <param name="manuallySetProperty">Property which value the user sets.</param>
		/// <param name="autofillProperty">Property which is autofilled.</param>
		/// <param name="autofillPropValue">Value which is autofilled.</param>
		/// <returns>Expected autofill messages for verification.</returns>
		private string[] GetAllowedAutofillDialogMessages(
			string manualPropValue,
			string manuallySetProperty,
			string autofillProperty,
			string autofillPropValue
			)
		{
			// Format the autofill message.
			string autofillDialogMsg = string.Format(
				AutofillDialogMessage,
				manualPropValue,
				manuallySetProperty,
				autofillProperty,
				autofillPropValue );

			// Format the autofill message for Edge browser. It has less space characters.
			string autoFillDialogMsgEdge = this.FormatAutofillMessageForEdge( autofillDialogMsg );

			// Return both allowed messages as an array.
			return new string[] { autofillDialogMsg, autoFillDialogMsgEdge };

		}

		/// <summary>
		/// Get allowed autofill dialog messages as an array of strings.
		/// The messages may be slightly different for some browsers, for example
		/// less space characters after line breaks, and therefore the verification
		/// should accept these different formats. This overloaded method expectes that
		/// 2 values are autofilled to the same property.
		/// </summary>
		/// <param name="manualPropValue">Value that the users sets.</param>
		/// <param name="manuallySetProperty">Property which value the user sets.</param>
		/// <param name="autofillProperty">Property which is autofilled.</param>
		/// <param name="autofillPropValueOne">First value which is autofilled.</param>
		/// <param name="autofillPropValueTwo">Second value which is autofilled.</param>
		/// <returns>Expected autofill messages for verification.</returns>
		private string[] GetAllowedAutofillDialogMessages(
			string manualPropValue,
			string manuallySetProperty,
			string autofillProperty,
			string autofillPropValueOne,
			string autofillPropValueTwo )
		{
			// Format the autofill message.
			string autofillDialogMsg = string.Format(
				AutofillPropertiesDialogMessage,
				manualPropValue,
				manuallySetProperty,
				autofillProperty,
				autofillPropValueOne,
				autofillPropValueTwo );

			// Format the autofill message for Edge browser. It has less space characters.
			string autoFillDialogMsgEdge = this.FormatAutofillMessageForEdge( autofillDialogMsg );

			// Return both allowed messages as an array.
			return new string[] { autofillDialogMsg, autoFillDialogMsgEdge };
		}

		public MetadataAutofill()
		{
			this.classID = "MetadataAutofill";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Metadata Autofill.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

		}

		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}

		[TearDown]
		public void EndTest()
		{
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Testing that the autoSetFilterProperty and the autofillProperty should be autofilled when property is selected to existing object.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Customer and Contact person autofill.docx",
			"Project",
			"Hospital Expansion (Miami, FL)",
			"Customer",
			"A&A Consulting (AEC)",
			"Contact person",
			"Ross Connors" )]
		public void AutofillPropertyAndAutoSetFilterPropertyOnExistingObject(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autoSetFilterProperty,
			string autoSetFilterPropValue,
			string autofillProperty,
			string autofillPropValue )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set property value and wait for it to trigger autofill dialog.
			MessageBoxDialog autofillDialog = metadataCardRightPane.Properties.SetPropertyValueAndWaitForAutofillDialog( manuallySetProperty, manualPropValue );

			// Verify that the autofill dialog contains expected message.
			string[] allowedAutofillMessages = this.GetAllowedAutofillDialogMessages(
				manualPropValue, manuallySetProperty, autofillProperty, autofillPropValue );
			Assert.That( autofillDialog.Message, Is.AnyOf( allowedAutofillMessages ),
				"Mismatch between expected and actual dialog message." );

			// Accept the dialog.
			autofillDialog.OKClick();

			// Verify that the manually set value is set as expected after accepting the autofill dialog.
			Assert.AreEqual( manualPropValue, metadataCardRightPane.Properties.GetPropertyValue( manuallySetProperty ),
				string.Format( SetPropertyValueMismatchMessage, manuallySetProperty ) );

			// Verify that property value is automatically set because of the value list filter in the manually set property.
			Assert.AreEqual( autoSetFilterPropValue, metadataCardRightPane.Properties.GetPropertyValue( autoSetFilterProperty ),
				string.Format( AutosetPropertyMismatchMessage, autoSetFilterProperty ) );

			// Verify that property is autofilled.
			Assert.AreEqual( autofillPropValue, metadataCardRightPane.Properties.GetPropertyValue( autofillProperty ),
				string.Format( AutofillPropertyMismatchMessage, autofillProperty ) );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

		}

		/// <summary>
		/// Testing that the autofillProperty should be autofilled 
		/// when property is selected to existing object.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Autofill Property.docx",
			"Project",
			"IT Training",
			"Contact person",
			"Michael Antoine" )]
		public void AutofillPropertyOnExistingObject(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autofillProperty,
			string autofillPropValue )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set property value and wait for it to trigger autofill dialog.
			MessageBoxDialog autofillDialog = metadataCardRightPane.Properties.SetPropertyValueAndWaitForAutofillDialog( manuallySetProperty, manualPropValue );

			// Verify that the autofill dialog contains expected message.
			string[] allowedAutofillMessages = this.GetAllowedAutofillDialogMessages(
				manualPropValue, manuallySetProperty, autofillProperty, autofillPropValue );
			Assert.That( autofillDialog.Message, Is.AnyOf( allowedAutofillMessages ),
				"Mismatch between expected and actual dialog message." );

			// Accept the dialog.
			autofillDialog.OKClick();

			// Verify that the manually set value is set as expected after accepting the autofill dialog.
			Assert.AreEqual( manualPropValue, metadataCardRightPane.Properties.GetPropertyValue( manuallySetProperty ),
				string.Format( SetPropertyValueMismatchMessage, manuallySetProperty ) );

			// Verify that property is autofilled.
			Assert.AreEqual( autofillPropValue, metadataCardRightPane.Properties.GetPropertyValue( autofillProperty ),
				string.Format( AutofillPropertyMismatchMessage, autofillProperty ) );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

		}

		/// <summary>
		/// Testing that the autoSetFilterProperty and the autofillProperty should be autofilled when Property is selected to New object.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Document",
			"Customer and Contact person autofill new object",
			"Microsoft PowerPoint Presentation (.pptx)",
			"Project",
			"Hospital Expansion (Miami, FL)",
			"Customer",
			"A&A Consulting (AEC)",
			"Contact person",
			"Ross Connors" )]
		public void AutofillPropertyAndAutoSetFilterPropertyOnNewObject(
			string objectType,
			string className,
			string objectName,
			string template,
			string manuallySetProperty,
			string manualPropValue,
			string autoSetFilterProperty,
			string autoSetFilterPropValue,
			string autofillProperty,
			string autofillPropValue )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open the new object metadata card from the template selector and 
			// starts creating a new object of specified object type and using the specified template.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set the name and class then add manuallySetProperty, autoSetFilterProperty and autofillProperty.
			newObjMDCard.Properties.SetPropertyValue( "Class", className );
			newObjMDCard.Properties.SetPropertyValue( "Name or title", objectName );
			newObjMDCard.Properties.AddProperty( manuallySetProperty );
			newObjMDCard.Properties.AddProperty( autoSetFilterProperty );
			newObjMDCard.Properties.AddProperty( autofillProperty );

			// Set property value and wait for it to trigger autofill dialog.
			MessageBoxDialog autofillDialog = newObjMDCard.Properties.SetPropertyValueAndWaitForAutofillDialog( manuallySetProperty, manualPropValue );

			// Verify that the autofill dialog contains expected message.
			string[] allowedAutofillMessages = this.GetAllowedAutofillDialogMessages(
				manualPropValue, manuallySetProperty, autofillProperty, autofillPropValue );
			Assert.That( autofillDialog.Message, Is.AnyOf( allowedAutofillMessages ),
				"Mismatch between expected and actual dialog message." );

			// Accept the dialog.
			autofillDialog.OKClick();

			// Verify that the manually set value is set as expected after accepting the autofill dialog.
			Assert.AreEqual( manualPropValue, newObjMDCard.Properties.GetPropertyValue( manuallySetProperty ),
				string.Format( SetPropertyValueMismatchMessage, manuallySetProperty ) );

			// Verify that property value is automatically set because of the value list filter in the manually set property. 
			Assert.AreEqual( autoSetFilterPropValue, newObjMDCard.Properties.GetPropertyValue( autoSetFilterProperty ),
				string.Format( AutosetPropertyMismatchMessage, autoSetFilterProperty ) );

			// Verify that property is autofilled.
			Assert.AreEqual( autofillPropValue, newObjMDCard.Properties.GetPropertyValue( autofillProperty ),
				string.Format( AutofillPropertyMismatchMessage, autofillProperty ) );

			// Save the object.
			newObjMDCard.SaveAndDiscardOperations.Save( false );

		}

		/// <summary>
		/// Hidden property is auto-filled
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Hidden project autofill.txt",
			"Employee",
			"Metadata autofill zero",
			"Project",
			"CRM Application Development",
			"(hidden)" )]
		public void AutofillWhenUserDoNotHaveAccessToTheObject(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autofillProperty,
			string autofillPropValueOne,
			string autofillPropValueTwo )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Set property value and wait for it to trigger autofill dialog.
			MessageBoxDialog autofillDialog = 
				mdCard.Properties.SetPropertyValueAndWaitForAutofillDialog( 
					manuallySetProperty, manualPropValue );

			// Verify that the autofill dialog contains expected message.
			string[] allowedAutofillMessages = this.GetAllowedAutofillDialogMessages(
				manualPropValue, manuallySetProperty, autofillProperty,
				autofillPropValueOne, autofillPropValueTwo );
			Assert.That( autofillDialog.Message, Is.AnyOf( allowedAutofillMessages ),
				"Mismatch between expected and actual dialog message." );

			// Accept the dialog.
			autofillDialog.OKClick();

			// Verify that property is autofilled.
			Assert.AreEqual( 
				autofillPropValueOne, 
				mdCard.Properties.GetMultiSelectLookupPropertyValueByIndex( autofillProperty, 0 ),
				string.Format( AutofillPropertyMismatchMessage, autofillProperty ) );
			Assert.AreEqual( 
				autofillPropValueTwo, 
				mdCard.Properties.GetMultiSelectLookupPropertyValueByIndex( autofillProperty, 1 ),
				string.Format( AutofillPropertyMismatchMessage, autofillProperty ) );
			
			// Save the operations.
			mdCard.SaveAndDiscardOperations.Save();

		}

		/// <summary>
		/// By default auto-filling does not overwrite existing values.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Auto fill for existing values.bmp",
			"Employee",
			"Metadata autofill zero",
			"Project",
			"Central Plains Area Development" )]
		public void AutofillDoesNotOverwriteExistingValue(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autofillProperty,
			string existingautofillPropValue )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set project value in Metadata Card.
			metadataCardRightPane.Properties.SetPropertyValue( manuallySetProperty, manualPropValue );

			// Verify that property is not autofilled/override, as property was already filled.
			Assert.AreEqual( existingautofillPropValue, metadataCardRightPane.Properties.GetPropertyValue( autofillProperty ),
				$"Unexpected change in value of property '{autofillProperty}'." );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();
		}

		/// <summary>
		/// Workflow is not auto-filled.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"workflow autofill.docx",
			"Project",
			"Project with Workflow",
			"Customer Project",
			"Customer",
			"In progress",
			"A&A Consulting (AEC)",
			"Yes",
			"Sample",
			"StateOne",
			6 )]
		public void WorkflowIsNotAutofilled(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manuallySetPropName,
			string manuallySetPropClassValue,
			string manuallySetPropertyRequiredPropOne,
			string manuallySetPropertyRequiredPropTwo,
			string manuallySetPropCustomerValue,
			string manuallySetPropInProgressValue,
			string workflow,
			string workflowState,
			int expectedPropertyCountAfterSettingClass )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Creating an property object to verify that the workflow autofill.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( manuallySetProperty );

			// Set the name class, name and required property.
			newObjMDCard.Properties.SetPropertyValue( "Class", manuallySetPropClassValue, expectedPropertyCountAfterSettingClass );
			newObjMDCard.Properties.SetPropertyValue( "Name or title", manuallySetPropName );
			newObjMDCard.Properties.SetPropertyValue( manuallySetPropertyRequiredPropOne, manuallySetPropCustomerValue );
			newObjMDCard.Properties.SetPropertyValue( manuallySetPropertyRequiredPropTwo, manuallySetPropInProgressValue );

			// Set the workflow.
			newObjMDCard.Workflows.SetWorkflow( workflow );

			// Click create button.
			newObjMDCard.SaveAndDiscardOperations.Save( false );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set property value in Metadata Card.
			metadataCardRightPane.Properties.SetPropertyValue( manuallySetProperty, manuallySetPropName );

			// Verify the workflow value is not set as expected in the right pane metadata card.
			Assert.AreEqual( "", metadataCardRightPane.Workflows.Workflow, "Mismatch between expected and actual workflow." );

			// Save the operations.
			metadataCardRightPane = metadataCardRightPane.SaveAndDiscardOperations.Save();

		}

		/// <summary>
		/// Testing that the Auto-fill question is not asked if it happens anyway due to filtered/linked property values.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Customer autofill.pub",
			"Project",
			"Philo District Redevelopment",
			"Customer",
			"City of Chicago (Planning and Development)" )]
		public void AutoSetFilterPropertyOnExistingObject(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autoSetFilterProperty,
			string autoSetFilterPropValue )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set property value in Metadata Card.
			metadataCardRightPane.Properties.SetPropertyValue( manuallySetProperty, manualPropValue );

			// Verify that the manually set value is set as expected.
			Assert.AreEqual( manualPropValue, metadataCardRightPane.Properties.GetPropertyValue( manuallySetProperty ), 
				string.Format( SetPropertyValueMismatchMessage, manuallySetProperty ) );

			// Verify that property value is automatically set because of the value list filter in the manually set property.
			Assert.AreEqual( autoSetFilterPropValue, metadataCardRightPane.Properties.GetPropertyValue( autoSetFilterProperty ), 
				string.Format( AutosetPropertyMismatchMessage, autoSetFilterProperty ) );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

		}

		/// <summary>
		/// Testing that the autofillProperty should not be autofilled when property is selected 
		/// and autofill dialog is discarded to existing object.
		/// </summary>
		[Test]
		[Category( "Autofill" )]
		[TestCase(
			"Document",
			"Discard autofill.pptx",
			"Project",
			"Hospital Expansion (Miami, FL)",
			"Customer",
			"A&A Consulting (AEC)",
			"Contact person",
			"Ross Connors" )]
		public void DiscardAutofillPropertyAndAutoSetFilterPropertyOnExistingObject(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autoSetFilterProperty,
			string autoSetFilterPropValue,
			string autofillProperty,
			string autofillPropValue )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set property value and wait for it to trigger autofill dialog.
			MessageBoxDialog autofillDialog = metadataCardRightPane.Properties.SetPropertyValueAndWaitForAutofillDialog( manuallySetProperty, manualPropValue );

			// Verify that the autofill dialog contains expected message.
			string[] allowedAutofillMessages = this.GetAllowedAutofillDialogMessages(
				manualPropValue, manuallySetProperty, autofillProperty, autofillPropValue );
			Assert.That( autofillDialog.Message, Is.AnyOf( allowedAutofillMessages ),
				"Mismatch between expected and actual dialog message." );

			// Decline the dialog.
			autofillDialog.CancelClick();

			// Verify that property value is automatically set because of the value list filter in the manually set property.
			Assert.AreEqual( autoSetFilterPropValue, metadataCardRightPane.Properties.GetPropertyValue( autoSetFilterProperty ),
				string.Format( AutosetPropertyMismatchMessage, autoSetFilterProperty ) );

			// Verify that property is not autofilled.
			Assert.AreEqual( "", metadataCardRightPane.Properties.GetPropertyValue( autofillProperty ),
				string.Format( AutofillPropertyMismatchMessage, autofillProperty ) );

			// Discard the operations.
			metadataCardRightPane.DiscardChanges();

		}

		/// <summary>
		/// Testing that the autoSetFilterProperty (Customer) and the autofillProperty (contact person) should be autofilled when property is selected to existing 
		/// object. Then Remove autoSetFilterProperty (Customer) value as we want to check if The project property (manuallySetProperty) should become empty when a 
		/// contact person property value selected which does not have any relationship with selected project value.
		/// In this case, changing value in Contact person triggers auto-set to Customer (because Customer is parent of Contact Person and thus Contact person 
		/// is filtered by the Customer). Customer value causes Project value to be emptied because the Project property is filtered by Customer AND the selected 
		/// Project value didn't reference to the selected Customer.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"Document",
			"ChainOfAutoSetFilterProperties.pptx",
			"Project",
			"Hospital Expansion (Miami, FL)",
			"Customer",
			"A&A Consulting (AEC)",
			"Contact person",
			"Ross Connors",
			"Debbie Smith" )]
		public void ChainOfAutoSetFilterProperties(
			string objectType,
			string objectName,
			string manuallySetProperty,
			string manualPropValue,
			string autoSetProperty,
			string autoSetPropertyValue,
			string autofillProperty,
			string autofillPropertyValue,
			string autofillPropertyValueTwo )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set manually property value in Metadata Card then Wait until message dialog box for autofill is loaded.
			MessageBoxDialog autofillDialog = metadataCardRightPane.Properties.SetPropertyValueAndWaitForAutofillDialog( manuallySetProperty, manualPropValue );

			// Verify that the autofill dialog contains expected message.
			string[] allowedAutofillMessages = this.GetAllowedAutofillDialogMessages(
				manualPropValue, manuallySetProperty, autofillProperty, autofillPropertyValue );
			Assert.That( autofillDialog.Message, Is.AnyOf( allowedAutofillMessages ),
				"Mismatch between expected and actual dialog message." );

			// Accept the dialog.
			autofillDialog.OKClick();

			// Verify the manually property value is set as expected in the right pane metadata card.
			Assert.AreEqual( manualPropValue, metadataCardRightPane.Properties.GetPropertyValue( manuallySetProperty ),
				"Mismatch between expected and actual manual set property" );

			// Verify the autofill property and auto set property in meta data card.
			Assert.AreEqual( autoSetPropertyValue, metadataCardRightPane.Properties.GetPropertyValue( autoSetProperty ),
				"Mismatch between expected and actual auto set value" );
			Assert.AreEqual( autofillPropertyValue, metadataCardRightPane.Properties.GetPropertyValue( autofillProperty ),
				"Mismatch between expected and actual autofill value." );

			// Remove auto set property value which should remove filter from the autofill property.
			metadataCardRightPane.Properties.RemoveMultiSelectLookupPropertyValue( autoSetProperty, autoSetPropertyValue );

			// Select new value to autofill property that has a different parent than the removed auto set property value (above).
			// Therefore, that different parent will be automatically set to the empty auto set property. Then, that value will
			// automatically clear the original manually set value if the manually set value doesn't have relationship to the auto set value.
			metadataCardRightPane.Properties.SetPropertyValue( autofillProperty, autofillPropertyValueTwo );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

			// Verify that the original manually set value becomes empty because the original value doesn't match the filter caused by the auto set value.
			Assert.IsEmpty( metadataCardRightPane.Properties.GetPropertyValue( manuallySetProperty ),
				"Manually set property is not empty." );

		}
	}
}
