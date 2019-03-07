using System.Collections.Generic;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -15 )]
	[Parallelizable( ParallelScope.Self )]
	class Assignments
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
		private static readonly string ObjectVersionMismatchMessage =
			"Mismatch between the expected and actual object version in the metadatacard.";
		private static readonly string AssigneeStatusMismatchMessage =
			"Mismatch between the expected and actual assignee status in the metadatacard.";
		private static readonly string ListViewAssignmentIconMismatchMessage =
			"Mismatch between the expected and actual assignment status icon in the list view.";
		private static readonly string MDCardAssignmentStatusIconMismatchMessage =
			"Mismatch between the expected and actual metadatacard header icon for assignment status.";
		private static readonly string AssignedToMeViewPresenceMessage =
			"Mismatch between the expected and actual object presence in the Assigned to me view.";
		private static readonly string CountMismatchInAssignedToPropMessage =
			"Mismatch between the expected and actual users count in 'Assigned to' property.";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public Assignments()
		{
			this.classID = "Assignments";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			//// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			//// TODO: Some environment details should probably come from configuration. For example the back end.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Assignments Vault.mfb", users );

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
		/// A wrapper method for getting the current method name.
		/// </summary>
		/// <returns>Current method name.</returns>
		private string GetCurrentMethodName()
		{
			return NUnit.Framework.TestContext.CurrentContext.Test.MethodName;
		}

		/// <summary>
		/// Frames the assignment name based on the current method name and date & time.
		/// </summary>
		/// <returns>Assignment name which is combined with Method name and current date & time.</returns>
		private string GetAssignmentName()
		{
			string objName = this.GetCurrentMethodName() + "-" + TimeHelper.GetCurrentDateAndTime( TimeHelper.TimeFormat.CustomFormat, "yyyy/MM/dd HH:mm:ss" );
			objName = objName.Replace( "/", "" ).Replace( ":", "" ).Replace( " ", "_" );
			return objName;
		}

		/// <summary>
		/// This method is used to create the new assignment object using the mentioned
		/// parameter values and returns the right pane metadatacard instance
		/// after it loaded.
		/// </summary>
		/// <param name="newObjMDCard">New assignment object metadatacard instance.</param>
		/// <param name="classValue">Assignment class value to be set in the metadatacard.</param>
		/// <param name="assignmentName">Assignment name value to be set in the metadatacard.</param>
		/// <param name="assignees">Assignment assignee values to be set in the metadatacard
		/// [For multiple assignees: ';' should be used to separate the assignees. For e.g.: Assignee1;Assignee2].</param>
		/// <returns></returns>
		private MetadataCardRightPane CreateNewAssignment( MetadataCardPopout newObjMDCard, string classValue, string assignmentName, string assignees,
			int expectedPropertyCountAfterSettingClass = 5 )
		{

			// Set class and name.
			newObjMDCard.Properties.SetPropertyValue( "Class", classValue, expectedPropertyCountAfterSettingClass );
			newObjMDCard.Properties.SetPropertyValue( "Name or title", assignmentName );

			// Get the assigned to users.
			string[] assignedToUsers = assignees.Split( ';' );

			// Set the assignee for each users by index in the metadatacard.
			for( int i = 0; i < assignedToUsers.Length; i++ )
				newObjMDCard.Properties.SetMultiSelectLookupPropertyValueByIndex( "Assigned to", assignedToUsers[ i ], i );

			// Click create button and get the right pane metadata card of the new assignment object.
			MetadataCardRightPane mdCard = newObjMDCard.SaveAndDiscardOperations.Save();

			return mdCard;

		} // end CreateNewAssignment

		/// <summary>
		/// This method is used to create the new assignment object using the mentioned
		/// parameter values and returns the right pane metadatacard instance
		/// without waiting for metadatacard loading in right pane.
		/// </summary>
		/// <param name="newObjMDCard">New assignment object metadatacard instance.</param>
		/// <param name="classValue">Assignment class value to be set in the metadatacard.</param>
		/// <param name="assignmentName">Assignment name value to be set in the metadatacard.</param>
		/// <param name="assignees">Assignment assignee values to be set in the metadatacard
		/// [For multiple assignees: ';' should be used to separate the assignees. For e.g.: Assignee1;Assignee2].</param>
		/// <returns></returns>
		private MetadataCardRightPane CreateNewAssignmentAndNotWaitForRightPaneMDCardLoad( MetadataCardPopout newObjMDCard, string classValue, string assignmentName, string assignees )
		{

			// Set class and name.
			newObjMDCard.Properties.SetPropertyValue( "Class", classValue );
			newObjMDCard.Properties.SetPropertyValue( "Name or title", assignmentName );

			// Get the assigned to users.
			string[] assignedToUsers = assignees.Split( ';' );

			// Set the assignee for each users by index in the metadatacard.
			for( int i = 0; i < assignedToUsers.Length; i++ )
				newObjMDCard.Properties.SetMultiSelectLookupPropertyValueByIndex( "Assigned to", assignedToUsers[ i ], i );

			// Click create button and get the right pane metadata card of the new assignment object.
			MetadataCardRightPane mdCard = newObjMDCard.SaveAndDiscardOperations.Save( false );

			return mdCard;

		} // end CreateNewAssignmentAndNotWaitForRightPaneMDCardLoad

		/// <summary>
		/// Testing that the assignment is mark completed for the current user as assignee.
		/// </summary>
		[Test, Order( 1 )]
		[TestCase(
			"Assignment",
			"Assignment that all must complete",
			Description = "Mark complete the Assignment of current user.",
			Category = "Smoke" )]
		public void MarkCompleteTheAssignmentOfCurrentUser(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the assigned to me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;

			// Mark complete the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( true, username );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save( false );

			// Check whether the assignment is removed from the assigned to me view once it is Completed in the metadatacard.
			Assert.AreEqual( false, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Verify that current user is mark completed the object in metadatacard.
			Assert.AreEqual( AssigneeStatus.Completed, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Completed icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, ObjectVersionMismatchMessage );

		}  // end MarkCompleteTheAssignmentOfCurrentUser

		/// <summary>
		/// Testing that the assignment is approved for the current user as assignee.
		/// </summary>
		[Test, Order( 2 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can approve",
			Description = "Approve the Assignment of current user.",
			Category = "Smoke" )]
		public void ApproveTheAssignmentOfCurrentUser(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the assigned to me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;

			// Approve the assignment in the metadatacard.
			mdCard.AssignmentOperations.Approve( true, username );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save( false );

			// Check whether the assignment is removed from the assigned to me view once it is Approved in the metadatacard.
			Assert.AreEqual( false, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Verify that current user is approved the assignment in metadatacard.
			Assert.AreEqual( AssigneeStatus.Approved, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Approved icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, ObjectVersionMismatchMessage );

		}  // end ApproveTheAssignmentOfCurrentUser

		/// <summary>
		/// Testing that the assignment is rejected for the current user as assignee.
		/// </summary>
		[Test, Order( 3 )]
		[TestCase(
			"Assignment",
			"Assignment that all must approve",
			Description = "Reject the Assignment of current user.",
			Category = "Smoke" )]
		public void RejectTheAssignmentOfCurrentUser(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the assigned to me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;

			// Reject the assignment in the metadatacard.
			mdCard.AssignmentOperations.Reject( true, username );

			// Save the changes.
			mdCard.SaveAndDiscardOperations.Save( false );

			// Check whether the assignment is removed from the assigned to me view once it is Rejected in the metadatacard.
			Assert.AreEqual( false, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Verify that current user is rejected the assignment in metadatacard.
			Assert.AreEqual( AssigneeStatus.Rejected, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Rejected icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.Rejected, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.Rejected, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, ObjectVersionMismatchMessage );

		}  // end RejectTheAssignmentOfCurrentUser

		/// <summary>
		/// Testing that user is able to uncomplete the assignment.
		/// </summary>
		[Test, Order( 4 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can complete",
			Description = "Un-complete the Assignment.",
			Category = "Smoke" )]
		public void UnCompleteTheAssignment(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Mark complete the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;

			// Un-complete the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( false, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify that the assignment is un-completed in metadatacard.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Completed icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.NotCompleted, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.NotCompleted, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, ObjectVersionMismatchMessage );

		}  // end UnCompleteTheAssignment	

		/// <summary>
		/// Testing that user is able to unapprove the assignment.
		/// </summary>
		[Test, Order( 5 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can approve",
			Description = "Un-Approve the Assignment.",
			Category = "Smoke" )]
		public void UnApproveTheAssignment(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Approve the assignment in the metadatacard.
			mdCard.AssignmentOperations.Approve( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;

			// Un-Approve the assignment in the metadatacard.
			mdCard.AssignmentOperations.Approve( false, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify that the assignment is un-approved in metadatacard.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Approved icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.NotCompleted, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.NotCompleted, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, ObjectVersionMismatchMessage );

		}  // end UnApproveTheAssignment	

		/// <summary>
		/// Testing that user is able to unreject the assignment.
		/// </summary>
		[Test, Order( 6 )]
		[TestCase(
			"Assignment",
			"Assignment that all must approve",
			Description = "Un-Reject the Assignment.",
			Category = "Smoke" )]
		public void UnRejectTheAssignment(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Reject the assignment in the metadatacard.
			mdCard.AssignmentOperations.Reject( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;

			// Un-Reject the assignment in the metadatacard.
			mdCard.AssignmentOperations.Reject( false, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify that the assignment is un-rejected in metadatacard.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Rejected icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.NotCompleted, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.NotCompleted, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, ObjectVersionMismatchMessage );

		}  // end UnRejectTheAssignment	

		/// <summary>
		/// Testing that the assignment is rejected when any one assignee 
		/// has rejected the assignment.
		/// </summary>
		[Test, Order( 7 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can approve",
			Description = "Reject the Assignment in Assignment that any one can approve object.",
			Category = "Smoke" )]
		public void CheckRejectActionInAnyOneCanApproveClass(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee name.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username + ";" + assignee );

			// Perform the assignment operation Reject.
			mdCard.AssignmentOperations.Reject( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the assigned to property values.
			var assignedToPropValues = mdCard.Properties.GetMultiSelectLookupPropertyValues( "Assigned to" );

			// Check whether the assigned to property have single assignee.
			Assert.AreEqual( 1, assignedToPropValues.Count, CountMismatchInAssignedToPropMessage );

			// Check other assignee is removed from the Assigned to Property.
			Assert.AreEqual( false, assignedToPropValues.Exists( e => e.Equals( assignee ) ),
				"Assigned to property still have the another user name '" + assignee +
				"' value after assignment is Approved/Completed by currently logged in user." );

			// Verify that the assignment is mark rejected in metadatacard.
			Assert.AreEqual( AssigneeStatus.Rejected, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Rejected icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.Rejected, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.Rejected, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

		}  // end CheckRejectActionInAnyOneCanApproveClass

		/// <summary>
		/// Testing that the assignment is rejected when any one assignee 
		/// has rejected the assignment.
		/// </summary>
		[Test, Order( 8 )]
		[TestCase(
			"Assignment",
			"Assignment that all must approve",
			Description = "Reject the Assignment in Assignment that all must approve class.",
			Category = "Smoke" )]
		public void CheckRejectActionInAllMustApproveClass(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee name.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username + ";" + assignee );

			// Perform the assignment operation Reject.
			mdCard.AssignmentOperations.Reject( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the assigned to property values.
			var assignedToPropValues = mdCard.Properties.GetMultiSelectLookupPropertyValues( "Assigned to" );

			// Check whether the assigned to property have single assignee.
			Assert.AreEqual( 1, assignedToPropValues.Count, CountMismatchInAssignedToPropMessage );

			// Check other assignee is removed from the Assigned to Property.
			Assert.AreEqual( false, assignedToPropValues.Exists( e => e.Equals( assignee ) ),
				"Assigned to property still have the another user name '" + assignee +
				"' value after assignment is Approved/Completed by currently logged in user." );

			// Verify that the assignment is mark rejected in metadatacard.
			Assert.AreEqual( AssigneeStatus.Rejected, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Rejected icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.Rejected, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.Rejected, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

		}  // end CheckRejectActionInAllMustApproveClass

		/// <summary>
		/// Testing that the assignment is mark completed when any one assignee 
		/// has completed the assignment.
		/// </summary>
		[Test, Order( 9 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can complete",
			Description = "Mark complete the Assignment in Assignment that any one can complete.",
			Category = "Smoke" )]
		public void CheckAssignmentWithAnyOneCanCompleteClass(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee name.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username + ";" + assignee );

			// Mark complete the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the assigned to property values.
			var assignedToPropValues = mdCard.Properties.GetMultiSelectLookupPropertyValues( "Assigned to" );

			// Check whether the assigned to property have single assignee.
			Assert.AreEqual( 1, assignedToPropValues.Count, CountMismatchInAssignedToPropMessage );

			// Check other assignee is removed from the Assigned to Property.
			Assert.AreEqual( false, assignedToPropValues.Exists( e => e.Equals( assignee ) ),
				"Assigned to property still have the another user name '" + assignee +
				"' value after assignment is Approved/Completed by currently logged in user." );

			// Verify that the assignment is completed in metadatacard.
			Assert.AreEqual( AssigneeStatus.Completed, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Completed icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

		}  // end CheckAssignmentWithAnyOneCanCompleteClass

		/// <summary>
		/// Testing that the assignment is approved when any one assignee 
		/// has approved the assignment.
		/// </summary>
		[Test, Order( 10 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can approve",
			Description = "Approve the Assignment in Assignment that any one can approve.",
			Category = "Smoke" )]
		public void CheckAssignmentWithAnyOneCanApproveClass(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee name.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username + ";" + assignee );

			// Approve the assignment in the metadatacard.
			mdCard.AssignmentOperations.Approve( true, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Get the assigned to property values.
			var assignedToPropValues = mdCard.Properties.GetMultiSelectLookupPropertyValues( "Assigned to" );

			// Check whether the assigned to property have single assignee.
			Assert.AreEqual( 1, assignedToPropValues.Count, CountMismatchInAssignedToPropMessage );

			// Check other assignee is removed from the Assigned to Property.
			Assert.AreEqual( false, assignedToPropValues.Exists( e => e.Equals( assignee ) ),
				"Assigned to property still have the another user name '" + assignee +
				"' value after assignment is Approved/Completed by currently logged in user." );

			// Verify that the assignment is approved in metadatacard.
			Assert.AreEqual( AssigneeStatus.Approved, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Approved icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

		}  // end CheckAssignmentWithAnyOneCanApproveClass

		/// <summary>
		/// Testing that the assignment is mark completed when all the assignees
		/// mark completed the assignment.
		/// </summary>
		[Test, Order( 11 )]
		[TestCase(
			"Assignment",
			"Assignment that all must complete",
			Description = "Assignment that all must complete.",
			Category = "Smoke" )]
		public void CheckAssignmentWithAllMustCompleteClass(
			string objectType,
			string className )
		{

			// Launches the driver and starts the test at home page as mentioned user credentials.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage( this.mfContext.UsernameOfUser( "vaultadmin" ), this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee name.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username + ";" + assignee );

			// Mark completes the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( true, assignee );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify that assignee is mark completed the assignment in metadatacard.
			Assert.AreEqual( AssigneeStatus.Completed, mdCard.AssignmentOperations.GetAssigneeStatus( assignee ), AssigneeStatusMismatchMessage );

			// Verify that Completed icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.NotCompleted, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.NotCompleted, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

			// Logout from the application and login as different user.
			LoginPage loginPage = homePage.TopPane.Logout();
			homePage = loginPage.Login( username, password, this.vaultName );

			// Navigates to the assigned to me view.
			listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Open the popped-out metadatacard.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Mark completes the assignment in the metadatacard.
			mdCardPopOut.AssignmentOperations.MarkComplete( true, username );

			// Save the changes.
			mdCardPopOut.SaveAndDiscardOperations.Save( false );

			// Check whether the assignment is not listed in the assigned to me view after performing mark complete.
			Assert.AreEqual( false, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Verify that the assignment is mark completed in metadatacard.
			Assert.AreEqual( AssigneeStatus.Completed, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Completed icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

		}  // end CheckAssignmentWithAllMustCompleteClass	

		/// <summary>
		/// Testing that the assignment is mark approved when all the assignees
		/// mark approved the assignment.
		/// </summary>
		[Test, Order( 12 )]
		[TestCase(
			"Assignment",
			"Assignment that all must approve",
			Description = "Assignment that all must approve.",
			Category = "Smoke" )]
		public void CheckAssignmentWithAllMustApproveClass(
			string objectType,
			string className )
		{

			// Launches the driver and starts the test at home page as mentioned user credentials.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage( this.mfContext.UsernameOfUser( "vaultadmin" ), this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee name.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objName, "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username + ";" + assignee );

			// Approve the assignment in the metadatacard.
			mdCard.AssignmentOperations.Approve( true, assignee );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify that assignee is approved the assignment in metadatacard.
			Assert.AreEqual( AssigneeStatus.Approved, mdCard.AssignmentOperations.GetAssigneeStatus( assignee ), AssigneeStatusMismatchMessage );

			// Verify that Approved icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.NotCompleted, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Logout from the application and login as different user.
			LoginPage loginPage = homePage.TopPane.Logout();
			homePage = loginPage.Login( username, password, this.vaultName );

			// Navigates to the assigned to me view.
			listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Open the popped-out metadatacard.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Approve the assignment in the metadatacard.
			mdCardPopOut.AssignmentOperations.Approve( true, username );

			// Save the changes.
			mdCardPopOut.SaveAndDiscardOperations.Save( false );

			// Check whether the assignment is not listed in the assigned to me view after performing mark approve.
			Assert.AreEqual( false, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Verify that the assignment is mark approved in metadatacard.
			Assert.AreEqual( AssigneeStatus.Approved, mdCard.AssignmentOperations.GetAssigneeStatus( username ), AssigneeStatusMismatchMessage );

			// Verify that Approved icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, MDCardAssignmentStatusIconMismatchMessage );

			// Verify the assignment icon in the list view.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, homePage.ListView.GetAssignmentStatus( objName ), ListViewAssignmentIconMismatchMessage );

		}  // end CheckAssignmentWithAllMustApproveClass			

		/// <summary>
		/// Testing that the warning message is displayed when the user tries to
		/// mark complete the assignment of different assignee.
		/// </summary>
		[Test, Order( 13 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can complete",
			Description = "Mark complete the Assignment of different user.",
			Category = "Smoke" )]
		public void MarkCompleteTheAssignmentOfDifferentUser(
			string objectType,
			string className )
		{

			// Launches the driver and starts the test at home page as mentioned user credentials.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage( this.mfContext.UsernameOfUser( "vaultadmin" ), this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Navigates to the assigned to me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, assignee );

			// Check whether the assignment is listed in the assigned to me view.
			Assert.AreEqual( true, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Logout from the application and login as different user.
			LoginPage loginPage = homePage.TopPane.Logout();
			homePage = loginPage.Login( this.username, this.password, this.vaultName );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Open the popped-out metadatacard.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Mark completes the assignment in the metadatacard.
			mdCardPopOut.AssignmentOperations.MarkComplete( true, assignee );

			// Save the changes and get the warning dialog instance.
			MessageBoxDialog messageBox = mdCardPopOut.SaveAndDiscardOperations.SaveAndWaitForMessageBoxDialog();

			string[] allowedWarningMessages = new string[]
			{
				// Other browsers.
				"Access denied.\r\nYou do not have sufficient access rights to mark the assignment as complete on behalf of other users.",

				// Edge.
				"Access denied.\r\n\r\nYou do not have sufficient access rights to mark the assignment as complete on behalf of other users."
			};

			// Verify that expected warning message is displayed.
			Assert.That( messageBox.Message, Is.AnyOf( allowedWarningMessages ),
				"Warning message is not correct or not shown." );
			
			// Close the message dialog and discard the changes.
			messageBox.OKClick();
			mdCard = mdCardPopOut.DiscardChanges();

			// Check whether the assignment is not mark completed by different user.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( assignee ),
				AssigneeStatusMismatchMessage );

		}  // end MarkCompleteTheAssignmentOfDifferentUser		

		/// <summary>
		/// Testing that the warning message is displayed when the user tries to
		/// approve the assignment of different assignee.
		/// </summary>
		[Test, Order( 14 )]
		[TestCase(
			"Assignment",
			"Assignment that all must approve",
			Description = "Approve the Assignment of different user.",
			Category = "Smoke" )]
		public void ApproveTheAssignmentOfDifferentUser(
			string objectType,
			string className )
		{

			// Launches the driver and starts the test at home page as mentioned user credentials.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage( this.mfContext.UsernameOfUser( "vaultadmin" ), this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Navigates to the assigned to me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, assignee );

			// Check whether the assignment is listed in the assigned to me view.
			Assert.AreEqual( true, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Logout from the application and login as different user.
			LoginPage loginPage = homePage.TopPane.Logout();
			homePage = loginPage.Login( this.username, this.password, this.vaultName );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Open the popped-out metadatacard.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Approve the assignment in the metadatacard.
			mdCardPopOut.AssignmentOperations.Approve( true, assignee );

			// Save the changes and get the warning dialog instance.
			MessageBoxDialog messageBox = mdCardPopOut.SaveAndDiscardOperations.SaveAndWaitForMessageBoxDialog();

			string[] allowedWarningMessages = new string[]
			{
				// Other browsers.
				"Access denied.\r\nYou do not have sufficient access rights to mark the assignment as complete on behalf of other users.",

				// Edge.
				"Access denied.\r\n\r\nYou do not have sufficient access rights to mark the assignment as complete on behalf of other users."
			};

			// Verify that expected warning message is displayed.
			Assert.That( messageBox.Message, Is.AnyOf( allowedWarningMessages ),
				"Warning message is not correct or not shown." );

			// Close the message dialog and discard the changes.
			messageBox.OKClick();
			mdCard = mdCardPopOut.DiscardChanges();

			// Check whether the assignment is not approved by different user.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( assignee ), AssigneeStatusMismatchMessage );

		}  // end ApproveTheAssignmentOfDifferentUser	

		/// <summary>
		/// Testing that the warning message is displayed when the user tries to
		/// reject the assignment of different assignee.
		/// </summary>
		[Test, Order( 15 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can approve",
			Description = "Reject the Assignment of current user.",
			Category = "Smoke" )]
		public void RejectTheAssignmentOfDifferentUser(
			string objectType,
			string className )
		{

			// Launches the driver and starts the test at home page as mentioned user credentials.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage( this.mfContext.UsernameOfUser( "vaultadmin" ), this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Navigates to the assigned to me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Get the assignee.
			string assignee = this.mfContext.UsernameOfUser( "vaultadmin" );

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, assignee );

			// Check whether the assignment is listed in the assigned to me view.
			Assert.AreEqual( true, homePage.ListView.IsItemInListing( objName ), AssignedToMeViewPresenceMessage );

			// Logout from the application and login as different user.
			LoginPage loginPage = homePage.TopPane.Logout();
			homePage = loginPage.Login( this.username, this.password, this.vaultName );

			// Navigates to the search view.
			listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Selects the object in the listing.
			mdCard = listing.SelectObject( objName );

			// Open the popped-out metadatacard.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Reject the assignment in the metadatacard.
			mdCardPopOut.AssignmentOperations.Reject( true, assignee );

			// Save the changes and get the warning dialog instance.
			MessageBoxDialog messageBox = mdCardPopOut.SaveAndDiscardOperations.SaveAndWaitForMessageBoxDialog();

			string[] allowedWarningMessages = new string[]
			{
				// Other browsers.
				"Access denied.\r\nYou do not have sufficient access rights to mark the assignment as rejected on behalf of other users.",

				// Edge.
				"Access denied.\r\n\r\nYou do not have sufficient access rights to mark the assignment as rejected on behalf of other users."
			};

			// Verify that expected warning message is displayed.
			Assert.That( messageBox.Message, Is.AnyOf( allowedWarningMessages ),
				"Warning message is not correct or not shown." );

			// Close the message dialog and discard the changes.
			messageBox.OKClick();
			mdCard = mdCardPopOut.DiscardChanges();

			// Check whether the assignment is not rejected by different user.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( assignee ), AssigneeStatusMismatchMessage );

		}  // end RejectTheAssignmentOfDifferentUser	

		/// <summary>
		/// Testing that the assignment version increases by one when user toggles the assignment as complete
		/// without saving and then toggle back as un-complete and save. Note that it is basically not "right"
		/// that the version increases but it is considered as OK.
		/// </summary>
		[Test, Order( 16 )]
		[TestCase(
			"Assignment",
			"Assignment that any one can complete",
			Category = "Smoke" )]
		public void CheckAssignmentVersionForNoChangeInState(
			string objectType,
			string className )
		{
			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Get the assignment name.
			string objName = this.GetAssignmentName();

			// Create the new assignment object.
			MetadataCardRightPane mdCard = this.CreateNewAssignment( newObjMDCard, className, objName, username );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Mark completes the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( true, username );

			// Un-completes the assignment in the metadatacard.
			mdCard.AssignmentOperations.MarkComplete( false, username );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify that assignment is not mark completed in metadatacard.
			Assert.AreEqual( AssigneeStatus.NotSelected, mdCard.AssignmentOperations.GetAssigneeStatus( username ),
				AssigneeStatusMismatchMessage );

			// Verify that Completed icon is not displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.NotCompleted, mdCard.Header.AssignmentStatus,
				MDCardAssignmentStatusIconMismatchMessage );

			// Check that the assignment version is increased by one.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version,
				ObjectVersionMismatchMessage );

		}  // end CheckAssignmentVersionForNoChangeInState

	}  // end Assignments
}