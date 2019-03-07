using System;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.Listing;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -12 )]
	[Category( "ExternalRepository" )]
	[Parallelizable( ParallelScope.Self )]
	class PromoteObjects
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		/// <summary>
		/// Assert messages for additional info.
		/// </summary>
		private static readonly string PromotedObjectVersionMismatch =
			"Mismatch between the expected and actual version of promoted object.";
		private static readonly string CreatedByUserMismatch =
			"Mismatch between the expected and actual created user in the promoted object metadata card.";
		private static readonly string LastModifieldByUserMismatch =
			"Mismatch between the expected and actual last modified user in the promoted object metadata card.";

		protected TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;


		public PromoteObjects()
		{
			this.classID = "PromoteObjects";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "NFC_sample_vault.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

			// Configure the Network Folder Connector in the vault.
			EnvironmentSetupHelper.ConfigureNetworkFolderConnectorToVault( this.mfContext, this.classID );

			// Promote objects in the vault.
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "sample_pdfa.pdf" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "article.aspx.txt", 1 );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "MultipleRecipientsInTOCC.msg" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "SubFolder3\\Asennusohje - M-Files laiterekisteri.docx" );
		}

		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( mfContext );

			EnvironmentSetupHelper.ClearExternalRepository( this.classID );
		}

		[TearDown]
		public void EndTest()
		{
			// Returns the browser to the home page to be used by the next test or quits the browser if
			// the test failed.
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Helper for printing error message when related object is not listed under relationships.
		/// </summary>
		private string FormatMissingRelatedObjectFailureMessage( string relationshipRootObject, string relationshipHeader, string relatedObject )
		{
			return String.Format( "Expected object '{0}' is not listed in relationships of object '{1}' under header '{2}'",
				relatedObject, relationshipRootObject, relationshipHeader );
		}

		/// <summary>
		/// Promote the object by changing the class in unmanaged object metadatacard.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase(
			"Income Statement 10 2006-6.xlsm",
			"Meeting Notice",
			Description = "Promoting unmanaged object in external view." )]
		[TestCase(
			"EmptyFields.msg",
			"Drawing",
			"subfolder3",
			"Reviewing drawings",
			"Listed for approval",
			Description = "Promoting unmanaged object inside sub folder in external view." )]
		[TestCase(
			"SubFolder3",
			"Other Document",
			Description = "Promoting unmanaged folder in external view." )]
		public void PromoteUnmangedObjectOrFolder(
			string unmanagedItemName,
			string className,
			string locationPropertyValue = "",
			string workflow = "",
			string workflowState = "" )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( unmanagedItemName );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedItemName );

			// Promote the object by changing the class.
			mdCard.Properties.SetPropertyValue( "Class", className );

			// Wait for workflow load if default workflow will set for the selected class.
			if( !workflow.Equals( "" ) )
			{
				mdCard.Workflows.WaitUntilWorkflowStateTransitionIconDisplayed();
			}

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that object version is displayed correctly.
			Assert.AreEqual( 1, mdCard.Header.Version, PromotedObjectVersionMismatch );

			// Assert that Created and Last modified user name field is updated correctly.
			Assert.AreEqual( "(external source)", mdCard.Header.CreatedBy, CreatedByUserMismatch );
			Assert.AreEqual( this.mfContext.UsernameOfUser( "user" ), mdCard.Header.LastModifiedBy, LastModifieldByUserMismatch );

			// Assert that metadata card of promoted object has the repository property with network location value.
			Assert.AreEqual( this.classID, mdCard.Properties.GetPropertyValue( "Repository" ),
				"Mismatch between the expected and actual repository property value." );

			// Assert that metadata card of promoted object has the location property with network location value if object from sub folder.
			if( !locationPropertyValue.Equals( "" ) )
			{
				Assert.AreEqual( locationPropertyValue, mdCard.Properties.GetPropertyValue( "Location" ),
					"Mismatch between the expected and actual location property value." );
			}

			// Assert that promoted object have default permission value.
			Assert.AreEqual( "Full control for all internal users", mdCard.Permissions.Permission,
				"Mismatch between the expected and actual permission in the metadata card." );

			// Assert the workflow and state value in the metadatacard.
			Assert.AreEqual( workflow, mdCard.Workflows.Workflow,
				"Mismatch between the expected and actual workflow set in the metadata card." );
			Assert.AreEqual( workflowState, mdCard.Workflows.WorkflowState,
				"Mismatch between the expected and actual workflow state set in the metadata card." );
		}

		/// <summary>
		/// Promote the object by changing the class in unmanaged object metadatacard with Relationship.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase(
			"Minutes Project Meeting 4 2007-7.docx",
			"Memo",
			"Project",
			"Austin District Redevelopment",
			"Projects",
			Description = "Promoting unmanaged object in external view with Relationship." )]
		[TestCase(
			"OrganizationOrg Chart.vstx",
			"Notice of Layoff or Termination",
			"Employee",
			"Kimberley Miller",
			"Employees",
			"subfolder2",
			Description = "Promoting unmanaged object inside sub folder in external view with Relationship." )]
		[TestCase(
			"SubFolder2",
			"Picture",
			"Customer",
			"CBH International",
			"Customers",
			Description = "Promoting unmanaged folder in external view with Relationship." )]
		public void PromoteUnmangedObjectOrFolderWithRelationship(
			string unmanagedItemName,
			string className,
			string relationshipProperty,
			string relationshipPropertyValue,
			string relationshipHeader,
			string locationPropertyValue = "" )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( unmanagedItemName );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedItemName );

			// Open the popped out metadatacard.
			MetadataCardPopout popedOutMdCard = mdCard.PopoutMetadataCard();

			// Promote the object by changing the class.
			popedOutMdCard.Properties.SetPropertyValue( "Class", className );
			popedOutMdCard.Properties.SetPropertyValue( relationshipProperty, relationshipPropertyValue );
			mdCard = popedOutMdCard.SaveAndDiscardOperations.Save();

			// Assert that object version is displayed correctly.
			Assert.AreEqual( 1, mdCard.Header.Version, PromotedObjectVersionMismatch );

			// Assert that Created and Last modified user name field is updated correctly.
			Assert.AreEqual( "(external source)", mdCard.Header.CreatedBy, CreatedByUserMismatch );
			Assert.AreEqual( this.mfContext.UsernameOfUser( "user" ), mdCard.Header.LastModifiedBy, LastModifieldByUserMismatch );

			// Assert that metadata card of promoted object has the repository property with network location value.
			Assert.AreEqual( this.classID, mdCard.Properties.GetPropertyValue( "Repository" ),
				"Mismatch between the expected and actual repository property value." );

			// Assert that metadata card of promoted object has the location property with network location value if object from sub folder.
			if( !locationPropertyValue.Equals( "" ) )
			{
				Assert.AreEqual( locationPropertyValue, mdCard.Properties.GetPropertyValue( "Location" ),
					"Mismatch between the expected and actual location property value." );
			}

			// Assert that promoted object have default permission value.
			Assert.AreEqual( "Full control for all internal users", mdCard.Permissions.Permission,
				"Mismatch between the expected and actual permission in the metadata card." );

			// Check the expected relationship is added for the promoted object in listing.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( unmanagedItemName );
			relationships = relationships.ExpandRelationships();

			// Assert that expected relationship header is displayed.
			Assert.True( relationships.IsRelationshipHeaderInRelationships( relationshipHeader ),
				"Mismatch between the expected and actual relationship header for the promoted object." );

			// Expand the relationship header.
			relationships = relationships.ExpandRelationshipHeader( relationshipHeader );

			// Assert that expected related object is displayed in relationship tree.
			Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relationshipPropertyValue ),
						this.FormatMissingRelatedObjectFailureMessage( unmanagedItemName, relationshipHeader, relationshipPropertyValue ) );
		}

		/// <summary>
		/// Search for the promoted object after promoting it in external view.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase( "ocrtestmfd_small.tif", "Document" )]
		public void SearchAfterPromotingObject( string unmanagedItemName, string className )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the external view.
			ListView listing = homePage.ListView.NavigateToView( this.classID );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedItemName );

			// Promote the object by changing the class.
			mdCard.Properties.SetPropertyValue( "Class", className );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Perform quick search for the promoted object.
			listing = homePage.SearchPane.QuickSearch( unmanagedItemName );

			// Assert that only one item is displayed in the list view.
			Assert.AreEqual( 1, listing.NumberOfItems, "Mismatch between the expected and actual number of items in the list view." );

			// Select the promoted object.
			mdCard = listing.SelectObject( unmanagedItemName );

			// Assert that object version is displayed correctly.
			Assert.AreEqual( 1, mdCard.Header.Version, PromotedObjectVersionMismatch );

			// Assert that Created and Last modified user name field is updated correctly.
			Assert.AreEqual( "(external source)", mdCard.Header.CreatedBy, CreatedByUserMismatch );
			Assert.AreEqual( this.mfContext.UsernameOfUser( "user" ), mdCard.Header.LastModifiedBy, LastModifieldByUserMismatch );

			// Assert that metadata card of promoted object has the repository property with network location value.
			Assert.AreEqual( this.classID, mdCard.Properties.GetPropertyValue( "Repository" ),
				"Mismatch between the expected and actual repository property value." );
		}

		/// <summary>
		/// Add relationship and workflow in the promoted object.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase(
			"Asennusohje - M-Files laiterekisteri.docx",
			"Project",
			"CRM Application Development",
			"Projects",
			"Processing job applications",
			"Job application received, awaiting review" )]
		public void AddRelationshipAndWorkflowInPromotedObject(
			string promotedObjectName,
			string relationshipProperty,
			string relationshipPropertyValue,
			string relationshipHeader,
			string workflow,
			string workflowState )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( promotedObjectName );

			// Select the promoted object.
			MetadataCardRightPane mdCard = listing.SelectObject( promotedObjectName );

			// Add the relationship to the object.
			mdCard.Properties.AddPropertyAndSetValue( relationshipProperty, relationshipPropertyValue );

			// Set the workflow.
			mdCard.Workflows.SetWorkflow( workflow );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save();

			// Check the expected relationship is added for the promoted object in listing.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( promotedObjectName );
			relationships = relationships.ExpandRelationships();

			// Assert that expected relationship header is displayed.
			Assert.True( relationships.IsRelationshipHeaderInRelationships( relationshipHeader ),
				"Mismatch between the expected and actual relationship header for the promoted object." );

			// Expand the relationship header.
			relationships = relationships.ExpandRelationshipHeader( relationshipHeader );

			// Assert that expected related object is displayed in relationship tree.
			Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relationshipPropertyValue ),
						this.FormatMissingRelatedObjectFailureMessage( promotedObjectName, relationshipHeader, relationshipPropertyValue ) );

			// Assert the workflow and state value in the metadatacard.
			Assert.AreEqual( workflow, mdCard.Workflows.Workflow,
				"Mismatch between the expected and actual workflow set in the metadata card." );
			Assert.AreEqual( workflowState, mdCard.Workflows.WorkflowState,
				"Mismatch between the expected and actual workflow state set in the metadata card." );
		}

		/// <summary>
		/// Checkout the promoted object and update the metadatacard and then undo-checkout the object
		/// and check whether changes are discarded successfully.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase(
			"article.aspx.txt",
			"Keywords",
			"Test keyword" )]
		public void CheckoutAndUndocheckoutPromotedObject(
			string promotedObjectName,
			string property,
			string propertyValue )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( promotedObjectName );

			// Checkout the promoted object.
			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( promotedObjectName );
			MetadataCardRightPane mdCard = contextMenu.CheckOutObject();

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version - 1;

			// Add the relationship to the object.
			mdCard.Properties.SetPropertyValue( property, propertyValue );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save();

			// Undo checkout the object.
			contextMenu = listing.RightClickItemOpenContextMenu( promotedObjectName );
			mdCard = contextMenu.UndoCheckOutObject();

			// Assert that object version is not increased.
			Assert.AreEqual( initialObjectVersion, mdCard.Header.Version, PromotedObjectVersionMismatch );

			// Assert that property value is not saved.
			Assert.IsEmpty( mdCard.Properties.GetPropertyValue( property ),
				"Mismatch between the expected and actual value after undo checkout the promoted object." );
		}

		/// <summary>
		/// Checkout the promoted object and update the metadatacard and then undo-checkout the object
		/// and check whether changes are discarded successfully.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase(
			"Bill Richards.docx",
			"Renamed",
			".docx" )]
		public void CheckoutAndUndocheckoutUnmanagedObject( 
			string unmanagedObjectName, 
			string rename,
			string fileExtension)
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( unmanagedObjectName );

			// Checkout the unmanaged object.
			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( unmanagedObjectName );
			MetadataCardRightPane mdCard = contextMenu.CheckOutObject();

			// Assert the checkout status in metadata card of the checked out unmanaged object.
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, mdCard.Header.CheckOutStatus,
				"Mismatch between the expected and actual checkout status of unmanaged object." );

			// Promote the object.
			string initialClassValue = mdCard.Properties.GetPropertyValue( "Name or title" );
			mdCard.Properties.SetPropertyValue( "Name or title", rename );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save();

			string renamedName = rename + fileExtension;

			// Undo checkout the object.
			contextMenu = listing.RightClickItemOpenContextMenu( renamedName );
			mdCard = contextMenu.UndoCheckOutObject();

			// Assert that changes are discarded.
			Assert.AreEqual( unmanagedObjectName, mdCard.Properties.GetPropertyValue( "Name or title" ) );
		}

		/// <summary>
		/// Click permission and check whether the expected warning message is displayed
		/// in unmanaged object metadatacard.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase( "wolol.ico" )]
		public void SetPermissionInUnmanagedObjectIsDenied( string unmanagedObjectName )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( unmanagedObjectName );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedObjectName );

			// Click on permission.
			MessageBoxDialog msgDialog = mdCard.Permissions.ClickPermissionSectionAndWaitForMessageDialog();

			// Assert that expected warning message is displayed.
			Assert.AreEqual( "You do not have permission-change access to this object. You can only view the current permissions.",
				msgDialog.Message, "Mismatch between the expected and actual warning message when clicking the permission section of the unmanaged object." );

			// Click OK in message dialog.
			msgDialog.OKClick();
		}

		/// <summary>
		/// Check comment field is not editable in the unmanaged object metadatacard.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase( "M-Files HR -myyntiesitys-3.pptm" )]
		public void AddCommentInUnmanagedObjectIsDenied( string unmanagedObjectName )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( unmanagedObjectName );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedObjectName );

			// Navigate to comments section in metadatacard.
			mdCard.Header.GoToComments();

			// Check comments field is not editable.
			Assert.False( mdCard.Comments.CheckCommentsTextBoxFieldIsEditable(),
				"Comments field is editable in the unmanaged object metadatacard." );
		}

		/// <summary>
		/// Check workflow field is not editable in the unmanaged object metadatacard.
		/// </summary>
		[Test]
		[Category( "PromoteObjects" )]
		[TestCase( "Convertable Excel File.xlsx" )]
		public void SetWorkflowInUnmanagedObjectIsDenied( string unmanagedObjectName )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.QuickSearch( unmanagedObjectName );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedObjectName );

			// Check workflow field is not editable.
			Assert.False( mdCard.Workflows.CheckWorkflowFieldIsEditable(),
				"Workflow field is editable in the unmanaged object metadatacard." );
		}

		/// <summary>
		/// User is able to Rename title of unmanaged object via metadata card.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "RGPP Partnership.txt",
			"Name Changed" )]
		public void RenameUnmanagedObjectViaMetaDataCard( string unmanagedItemName,
			string unmanagedNewName )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the external view.
			ListView listing = homePage.ListView.NavigateToView( this.classID );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedItemName );

			// Change the name of unmanaged object.
			mdCard.Properties.SetPropertyValue( "Name or title", unmanagedNewName );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify the Name is displayed as expected in the right pane metadata card.
			Assert.AreEqual( unmanagedNewName, mdCard.Properties.GetPropertyValue( "Name or title" ),
				"Expected name value did not match the actual value" );
		}

		/// <summary>
		/// User is able to Rename title of managed object via metadata card.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "Document",
			"Parrot.jpg",
			"New Name" )]
		public void RenameManageObjectViaMetaDataCard(
			string className,
			string unmanagedItemName,
			string managedNewName )
		{
			// Start the test at home page as default user.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Navigate to the external view.
			ListView listing = homePage.ListView.NavigateToView( this.classID );

			// Select the unmanaged object.
			MetadataCardRightPane mdCard = listing.SelectObject( unmanagedItemName );

			// Promote the object by changing the class.
			mdCard.Properties.SetPropertyValue( "Class", className );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Change the name of managed object.
			mdCard.Properties.SetPropertyValue( "Name or title", managedNewName );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify the Name is displayed as expected in the right pane metadata card.
			Assert.AreEqual( managedNewName, mdCard.Properties.GetPropertyValue( "Name or title" ),
				"Expected name value did not match the actual value" );
		}
	}
}
