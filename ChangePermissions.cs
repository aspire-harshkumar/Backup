using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -8 )]
	[Parallelizable( ParallelScope.Self )]
	public class ChangePermissions
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID = "ChangePermissions";

		/// <summary>
		/// The configurations for the test class.
		/// </summary>
		private TestClassConfiguration configuration;

		/// <summary>
		/// The browser manager for test class.
		/// </summary>
		private TestClassBrowserManager browserManager;

		/// <summary>
		/// Test context information, including vault connections.
		/// </summary>
		private MFilesContext mfContext;

		/// <summary>
		/// The name of the test vault.
		/// </summary>
		private string VaultName => this.mfContext.VaultName;

		/// <summary>
		/// The username of the default user.
		/// </summary>
		private string Username => this.mfContext.UsernameOfUser( "user" );

		/// <summary>
		/// The password of the default user.
		/// </summary>
		private string Password => this.mfContext.PasswordOfUser( "user" );

		/// <summary>
		/// Initializes the test environment.
		/// </summary>
		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetTestUsers();

			// Initialize the test environment.
			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment(
					EnvironmentHelper.VaultBackend.Firebird,
					"Data Types And Test Objects.mfb",
					users );

			// Initialize browser manager with default users.
			this.browserManager = new TestClassBrowserManager(
					this.configuration,
					this.Username,
					this.Password,
					this.VaultName );
		}

		/// <summary>
		/// Tear downs the test environment.
		/// </summary>
		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			// Quit the existing browser.
			this.browserManager.EnsureQuitBrowser();

			// Delete the test environment.
			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}

		/// <summary>
		/// Finalize browser state after test is failed or succeed.
		/// </summary>
		[TearDown]
		public void EndTest()
		{
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Tests that current permission is displayed correctly in every possible place.
		/// </summary>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of the test object.</param>
		/// <param name="expectedNacl">The expected named access control list of the test object</param>
		/// <param name="expectedNaclInDialog">
		///		The expected named access control list seen in dialog if it is different as expextedNacl.
		///	</param>
		[Category( "Smoke" )]
		[TestCase(
			"Document",
			"Request for Proposal - Graphical Design.doc",
			"Full control for all internal users" )]
		[TestCase(
			"Document",
			"Custom permissions.xlsx",
			"Custom" )]
		[TestCase(
			"Document",
			"Income Statement 11/2006.xls",
			"Full control for all internal users",
			Description = "De-activated automatic permissions.")]
		[TestCase(
			"Document",
			"Automatic permissions change permission.xlsx",
			"Automatic permissions from object properties",
			"Custom",
			Description = "Active automatic permissions.")]
		[TestCase(
			"Assignment",
			"Preview assignment",
			"Full control for all internal users" )]
		[TestCase(
			"Document collection",
			"Floor Plans / Central Plains",
			"Full control for all internal users" )]
		public void PermissionIsDisplayedCorrectly(
			string objectType,
			string objectName,
			string expectedNacl,
			string expectedNaclInDialog = null
		)
		{
			// If expectedNaclInDialog is null the same NACL is 
			// is expected to see everywhere.
			expectedNaclInDialog = expectedNaclInDialog ?? expectedNacl;

			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Verify that permission is shown correctly in the metadata card.
			string displayedPermissionInMetadataCard = metadataCard.Permissions.Permission;
			Assert.AreEqual( expectedNacl, displayedPermissionInMetadataCard,
					$"Permission is shown incorrectly in metadata card: {objectName}" );

			// Verify that permission is shown correctly in the change dialog.
			string displayedPermissionInDialog = metadataCard.Permissions.GetPermissionFromDialog();
			Assert.AreEqual( expectedNaclInDialog, displayedPermissionInDialog,
					$"Permission is shown incorrectly in the dialog: {objectName}" );

			// Popout metadata card and do same tests.
			MetadataCardPopout metadataCardPopout = metadataCard.PopoutMetadataCard();

			// Verify that permission is shown correctly in the popped out metadata card.
			string displayedPermissionMetadataCardPopOut = metadataCardPopout.Permissions.Permission;
			Assert.AreEqual( expectedNacl, displayedPermissionMetadataCardPopOut,
					$"Permission is shown incorrectly in the popped out metadata card: {objectName}" );

			// Verify that permission is shown correctly in the change dialog that is opened 
			// via popped out metadata card. 
			string displayedPermissionDialogPoppout = metadataCardPopout.Permissions.GetPermissionFromDialog();
			Assert.AreEqual( expectedNaclInDialog, displayedPermissionDialogPoppout,
					$"Permission is shown incorrectly in the dialog that has been opened via popped out metadata card: {objectName}" );

			// Close the popped out metadata card.
			metadataCardPopout.CloseButtonClick();
		}



		/// <summary>
		/// The permission is changed to a permission, which does not change permissions of the test 
		/// user via metadata card in right pane. Tests that correct information is displayed after 
		/// change and after the object saved.
		/// </summary>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of the test object.</param>
		/// <param name="newNamedAcl">
		///		The new named access control list for test object that is different as its current 
		///		named access control list.
		///	</param>
		[Category( "Smoke" )]
		[TestCase(
			"Document",
			"Door Chart 01E.dwg",
			"Only for me" )]
		public void ChangePermissionsFromRightPaneMetadataCard(
			string objectType,
			string objectName,
			string newNamedAcl
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Get current named access control list.
			string previousNacl = metadataCard.Permissions.Permission;

			// Set permission to "Only for me" and wait that metadata card is loaded.
			metadataCard.Permissions.SetPermission( newNamedAcl );

			// Verify that the changed NACL is changed and displayed correctly 
			// in metadata card.
			string currentNacl = metadataCard.Permissions.Permission;
			Assert.AreEqual( newNamedAcl, currentNacl,
					$"Incorrect NACL is displayed in metadata card after change: {objectName}" );

			// Save changes. Verify that NACL is changed. Version should not be changed.
			int versionBeforeSave = metadataCard.Header.Version;
			metadataCard.SaveAndDiscardOperations.Save();
			currentNacl = metadataCard.Permissions.Permission;
			Assert.AreNotEqual( previousNacl, currentNacl,
					"New NACL is not taken in use. Previous NACL is still visible." );
			Assert.AreEqual( newNamedAcl, currentNacl,
					$"Incorrect NACL is displayed in metadata card after save: {objectName}" );
			int versionAfterSave = metadataCard.Header.Version;
			Assert.AreEqual( versionBeforeSave, versionAfterSave,
					$"Version is changed even if only permissions is modified: {objectName}" );
		}

		/// <summary>
		/// The permission is changed to a permission, which does not change permissions of the test 
		/// user via popped out metadata card. Tests that correct information is displayed after 
		/// change and after the object saved.
		/// </summary>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of these test object.</param>
		/// <param name="newNamedAcl">
		///		The new named access control list for test object that is different as its current 
		///		named access control list and do not change permissions of the user.
		///	</param>
		[Category( "Smoke" )]
		[TestCase(
			"Document",
			"Door Chart 05E.dwg",
			"Only for me" )]
		public void ChangePermissionFromPoppedOutMetadataCard(
			string objectType,
			string objectName,
			string newNamedAcl
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );
			;

			// Get current named access control list.
			string previousNacl = metadataCardRightPane.Permissions.Permission;

			// Popout metadata card.
			MetadataCardPopout metadataCardPopout = metadataCardRightPane.PopoutMetadataCard();

			// Set permission to "Only for me" and wait that metadata card is loaded.
			metadataCardPopout.Permissions.SetPermission( newNamedAcl );

			// Verify that the changed NACL is displayed in metadata card.
			string currentNacl = metadataCardPopout.Permissions.Permission;
			Assert.AreEqual( newNamedAcl, currentNacl,
					"Incorrect NACL is displayed in popped out metadata card after change." );

			// Click save in popped out metadata card. Verify that version
			// is not changed when only permission is changed.
			int versionBeforeSave = metadataCardPopout.Header.Version;
			metadataCardRightPane = metadataCardPopout.SaveAndDiscardOperations.Save();
			currentNacl = metadataCardRightPane.Permissions.Permission;
			int versionAfterSave = metadataCardRightPane.Header.Version;
			Assert.AreNotEqual( previousNacl, currentNacl,
					"New NACL is not taken in use after save. Previous NACL is still visible." );
			Assert.AreEqual( newNamedAcl, currentNacl,
					"Incorrect NACL is displayed in metadata card after save." );
			Assert.AreEqual( versionBeforeSave, versionAfterSave,
					"Version is changed even if only permissions are modified." );
		}

		/// <summary>
		/// Tests a situation where the user cannot see the object after user
		/// has itself changed permissions.
		/// </summary>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of the test object.</param>
		[Category( "Smoke" )]
		[TestCase(
			"Document",
			"Project Plan / Feasibility Study.doc" )]
		public void ObjectVanishAfterPermissionChange(
			string objectType,
			string objectName
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search test object by name.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set permission to "Visible to company management only" but do not wait metadata card because it
			// should not be visible after permissions are changed.
			metadataCardRightPane.Permissions.SetPermission( "Visible to company management only" );
			metadataCardRightPane.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

			// The metadata card should not be visible anymore.
			Assert.IsFalse( metadataCardRightPane.IsLoaded,
					"Metadata card of the object is still visible." );

			// The object should be vanished from the listing.
			Assert.IsFalse( listing.IsItemInListing( objectName ),
					"The object is still visible in the listing." );

		}

		/// <summary>
		/// Tests a situation where user lose edit permission.
		/// </summary>
		/// <param name="newNamedAcl">Named access control list that removes edit permission.</param>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of the test object.</param>
		[Category( "Smoke" )]
		[TestCase(
			"Read only for all internal users",
			"Document",
			"Project Plan / Records Management.doc",
			Description = "Document with permissions: Read only for all internal users." )]
		[TestCase(
			"Allow read to current user",
			"Document",
			"Allow Read to Current User.docx",
			Description = "Document with permissions: Allow read to current user." )]
		[TestCase(
			"Deny edit allow read to current user",
			"Document",
			"Deny Edit and Allow Read to Current User.ppt",
			Description = "Document with permissions: Deny edit but allow read to current user." )]

		public void NoEditPermissions(
			string newNamedAcl,
			string objectType,
			string objectName
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search the test object by name.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = listing.SelectObject( objectName );

			// Set permission to permission that removes all except read permission.
			metadataCard.Permissions.SetPermission( newNamedAcl );
			metadataCard.SaveAndDiscardOperations.Save();

			// Check that any properties cannot be modified.
			PropertiesInMetadataCard properties = metadataCard.Properties;
			foreach( string property in properties.PropertyNames )
				Assert.IsFalse( properties.CanPropertyEditModeBeActivated( property ),
						"Properties can be modified without edit permission." );

			// Verify correct permission is displayed in the metadata card.
			Assert.AreEqual( newNamedAcl, metadataCard.Permissions.Permission,
				"Correct NACL is not shown in the metadata card." );

			// Try to checkout the object via list view. Warning message should be appeared.
			MessageBoxDialog warningDialogContextMenu = listing
					.RightClickItemOpenContextMenu( objectName )
					.CheckOutObjectWaitDialog();

			string[] allowedErrorMessages = new string[]
			{
				// Edge.
				"Access denied.\r\n\r\nYou do not have sufficient access rights to the object.",
				
				// Other Browsers.
				"Access denied.\r\nYou do not have sufficient access rights to the object."
			};

			// Verify that warning message is correct.
			Assert.That( warningDialogContextMenu.Message, Is.AnyOf( allowedErrorMessages ),
				"Warning message is not correct or not shown." );

			// Close the message box dialog.
			warningDialogContextMenu.OKClick();
		}

		/// <summary>
		/// Tests a situation where the user lose change permission.
		/// </summary>
		/// <param name="newNamedAcl">
		///		The named access control list where permission is changed and 
		///		it removes change permission.
		/// </param>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of the test object.</param>
		[Category( "Smoke" )]
		[TestCase(
			"Read only for all internal users",
			"Document",
			"Invoice LCC556-4.pdf" )]
		[TestCase(
			"Read and edit permissions for all internal users",
			"Document",
			"Sales Invoice Template.xls" )]
		public void NoChangePermissionsPermission(
			string newNamedAcl,
			string objectType,
			string objectName
		)
		{

			string expectedMessage =
				"You do not have permission-change access to this object. You can only view the current permissions.";

			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			metadataCard.Permissions.SetPermission( newNamedAcl );
			metadataCard.SaveAndDiscardOperations.Save();

			// Click permissions section to get informative message that permissions cannot be changed.
			MessageBoxDialog messageBox =
				metadataCard.Permissions.ClickPermissionSectionAndWaitForMessageDialog();

			// Assert that permission cannot be modified and there is a message.
			Assert.AreEqual( expectedMessage, messageBox.Message, "Mismatch between expected and actual message." );

			messageBox.OKClick();

			// Verify that the displayed permission is displayed as expected.
			Assert.AreEqual( newNamedAcl, metadataCard.Permissions.Permission );

			// Try to change permissions via popped out metadata card.
			MetadataCardPopout metadataCardPopout = metadataCard.PopoutMetadataCard();

			// Click permissions section to get informative message that permissions cannot be changed.
			messageBox = metadataCardPopout.Permissions.ClickPermissionSectionAndWaitForMessageDialog();

			// Assert that permission cannot be modified and there is a message.
			Assert.AreEqual( expectedMessage, messageBox.Message, "Mismatch between expected and actual message." );

			messageBox.OKClick();

			// Verify that the displayed permission is displayed as expected.
			Assert.AreEqual( newNamedAcl, metadataCard.Permissions.Permission );

			// Close the popped out metadata card.
			metadataCardPopout.CloseButtonClick();
		}

		/// <summary>
		/// Tests a situation where user change permissions and properties simultaneously.
		/// </summary>
		/// <param name="newNamedAcl"> The named access control list where permission is changed. </param>
		/// <param name="objectType">The object type of the test object.</param>
		/// <param name="objectName">The name of the test object.</param>
		[Category( "Smoke" )]
		[TestCase(
			"Only for me",
			"Document",
			"Invitation to Project Meeting 1/2004.doc" )]
		[TestCase(
			"Read only for all internal users",
			"Document",
			"Invitation to Project Meeting 1/2006.doc" )]
		[TestCase(
			"Read and edit permissions for all internal users",
			"Document",
			"Invitation to Project Meeting 1/2007.doc" )]
		public void PermissionAndPropertiesChanged(
			string newNamedAcl,
			string objectType,
			string objectName
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Set permission to permission that removes change permissions permission.
			metadataCard.Permissions.SetPermission( newNamedAcl );

			// Modify name or title and keywords.
			PropertiesInMetadataCard properties = metadataCard.Properties;
			properties.SetPropertyValue( "Name or title", Guid.NewGuid().ToString( "N" ) );
			properties.SetPropertyValue( "Keywords", Guid.NewGuid().ToString( "N" ) );

			// Save changes and verify that the version is increased correctly.
			int versionBeforeSave = metadataCard.Header.Version;
			metadataCard.SaveAndDiscardOperations.Save();
			int versionAfterSave = metadataCard.Header.Version;
			Assert.AreEqual( versionBeforeSave + 1, versionAfterSave,
					"The version is not increased correctly." );
		}
	}
}
