using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.Listing;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -10 )]
	[Parallelizable( ParallelScope.Self )]
	class WorkflowAssignments
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		// Variable declaration of Commonly used values across tests.
		private static readonly string WorkflowAssignmentRelationshipHeader = "Workflow Assignment";
		private static readonly string AssignedToProperty = "Assigned to";
		private static readonly string ServerUserName = "(M-Files Server)";

		// Additional assert failure messages.
		private static readonly string VersionMismatch = "Mismatch between the expected and actual object version after automatic state transition.";
		private static readonly string AutomaticStateTransitionMismatch = "Mismatch between the expected and actual workflow state transition after {0} the workflow assignment(s).";
		private static readonly string LastModifiedUserMismatch = "Mismatch between the expected and actual last modified by user in metadatacard header after automatic state transition.";
		private static readonly string AssigneeStatusMismatch = "Mismatch between the expected and actual assignment status in the metadatacard.";
		private static readonly string AssignmentStatusIconMismatchMessage = "Mismatch between the expected and actual assignment status icon in metadata card header.";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public WorkflowAssignments()
		{
			this.classID = "WorkflowAssignments";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			//// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetTestUsers();

			//// TODO: Some environment details should probably come from configuration. For example the back end.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Workflow Assignments.mfb", users );

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
		/// This function is used to the search and select the object and set the workflow for the selected object.
		/// </summary>
		/// <returns>Returns the updated metadatacard instance after setting the workflow.</returns>
		private MetadataCardRightPane SearchObjectAndSetWorkflow( HomePage homePage, string objectName, string objectType, string workflow )
		{
			// Navigate to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Set the workflow.
			mdCard.Workflows.SetWorkflow( workflow );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Return the metadatacard instance.
			return mdCard;
		}

		/// <summary>
		/// This function is used to expand the relationship of the object and select the related item in the relationship tree.
		/// </summary>
		/// <returns>Related object metadatacard instance after selecting that in the list  view.</returns>
		private MetadataCardRightPane ExpandRelationshipAndSelectRelatedObject( ListView listing, string objectName, string relatedObjName,
			string WorkflowAssignmentRelationshipHeader = "Workflow Assignment" )
		{
			// Expand the relationship tree of the object.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName ).ExpandRelationships();

			// Expand the workflow assignment relationship header.
			relationships = relationships.ExpandRelationshipHeader( WorkflowAssignmentRelationshipHeader );

			// Select the workflow assignment in the relationship tree and return the metadatacard instance.
			return relationships.SelectRelatedObject( WorkflowAssignmentRelationshipHeader, relatedObjName );
		}

		/// <summary>
		/// This function is used to selects the object after performing the filtered search and returns the metadatacard instance of the selected object.
		/// </summary>
		/// <returns>Right pane metadatacard instance of the selected object.</returns>
		private MetadataCardRightPane SearchAndSelectObject( HomePage homePage, string objectName, string objectType )
		{
			// Navigate to the home page.
			homePage = homePage.TopPane.TabButtons.HomeTabClick();

			// Search for the object again.
			homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object and return the metadatacard instance.
			return homePage.ListView.SelectObject( objectName );
		}

		/// <summary>
		/// This function is used to assert the required details with the metadatacard of the automatic state transition performed object.
		/// </summary>
		private void AssertAutomaticStateTransitionObject( string expectedAutomaticStateTransition, int expectedObjectVersion, string expectedModifiedUser,
			MetadataCardRightPane mdCard, string assertMessage )
		{
			// Assert that automatic state transition is performed.
			Assert.AreEqual( expectedAutomaticStateTransition, mdCard.Workflows.WorkflowState, assertMessage );

			// Assert that object version is increased.
			Assert.AreEqual( expectedObjectVersion, mdCard.Header.Version, VersionMismatch );

			// Assert that last modified is updated correctly.
			Assert.AreEqual( expectedModifiedUser, mdCard.Header.LastModifiedBy, LastModifiedUserMismatch );
		}

		/// <summary>
		/// This function is used to assert the required details with the MetadataCard of the assignment object.
		/// </summary>
		private void AssertCompletedOrApprovedAssignmentDetails( string expectedAssigneeStatus, string assignmentName, MetadataCardRightPane mdCard )
		{
			// Based on the expected status assert the assignment status in metadatacard.
			if( expectedAssigneeStatus.Equals( "Completed" ) )
			{
				Assert.AreEqual( AssigneeStatus.Completed, mdCard.AssignmentOperations.GetAssigneeStatus( username ),
					AssigneeStatusMismatch );
			}
			else
			{
				Assert.AreEqual( AssigneeStatus.Approved, mdCard.AssignmentOperations.GetAssigneeStatus( username ),
					AssigneeStatusMismatch );
			}

			// Verify that Completed icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.CompletedOrApproved, mdCard.Header.AssignmentStatus, AssignmentStatusIconMismatchMessage );

		}

		/// <summary>
		/// This function is used to assert the required details with the MetadataCard of the assignment object.
		/// </summary>
		private void AssertRejectedAssignmentDetails( string assignmentName, MetadataCardRightPane mdCard )
		{
			// Verify that current user is rejected the assignment in metadatacard.
			Assert.AreEqual( AssigneeStatus.Rejected, mdCard.AssignmentOperations.GetAssigneeStatus( username ),
				AssigneeStatusMismatch );

			// Verify that Rejected icon is displayed in the Metadatacard header icon.
			Assert.AreEqual( AssignmentStatus.Rejected, mdCard.Header.AssignmentStatus, AssignmentStatusIconMismatchMessage );
		}

		/// <summary>
		/// Testing that the workflow assignment is assigned to the user based on the state transition performed user
		/// and expected deadline, description, monitored by property values are set.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Requirements Specification - Southwest Power",
			"Document",
			"SeparateAssignment Workflow" )]
		public void CheckSeparateAssignmentCreated(
			string objectName,
			string objectType,
			string workflow )
		{
			// Get the workflow assignment name.
			string assignmentName = "Assignment: " + objectName.Split( '.' )[ 0 ];

			// Frame the assignment description.
			string expectedDescription = "Name: " + objectName.Split( '.' )[ 0 ] + ";Keyword: {0}";

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the keywords property value.
			string keywords = mdCard.Properties.GetPropertyValue( "Keywords" );

			// Set the workflow.
			mdCard.Workflows.SetWorkflow( workflow );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Expand the relationship tree of the object.
			RelationshipsTree relationships = homePage.ListView.GetRelationshipsTreeOfObject( objectName ).ExpandRelationships();

			// Assert that workflow assignment relationship header is displayed in listing.
			Assert.True( relationships.IsRelationshipHeaderInRelationships( WorkflowAssignmentRelationshipHeader ) );

			// Expand the workflow assignment relationship header.
			relationships = relationships.ExpandRelationshipHeader( WorkflowAssignmentRelationshipHeader );

			// Select the workflow assignment in the relationship tree.
			mdCard = relationships.SelectRelatedObject( WorkflowAssignmentRelationshipHeader, assignmentName );

			// Assert that workflow assignment is assigned to the user based on the state transition performed user.
			Assert.AreEqual( username, mdCard.Properties.GetPropertyValue( AssignedToProperty ),
				"Mismatch between the expected and actual assigned to user in workflow assignment." );

			// Assert that workflow assignment is monitored by the user based on the state transition performed user.
			Assert.AreEqual( username, mdCard.Properties.GetPropertyValue( "Monitored by" ),
				"Mismatch between the expected and actual monitored by user in workflow assignment." );

			// Assert that workflow assignment has the expected assignment description.
			Assert.AreEqual( string.Format( expectedDescription, keywords ), mdCard.Properties.GetPropertyValue( "Assignment description" ),
				"Mismatch between the expected and actual assignment description in workflow assignment." );

			// Assert that workflow assignment has the expected deadline.
			Assert.AreEqual( TimeHelper.GetModifiedDate( 0, 10, 0 ), mdCard.Properties.GetPropertyValue( "Deadline" ),
				"Mismatch between the expected and actual deadline in workflow assignment." );

		}

		/// <summary>
		/// Testing that the workflow assignment is assigned to the user based on the metadatacard property.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Training Slides - Day 2.ppt",
			"Document",
			"M-Files user",
			"AssignedToFromMetadataProp Workflow" )]
		public void CheckSeparateAssignmentAssignedToSpecificUserBasedOnMetadataProperty(
			string objectName,
			string objectType,
			string propertyName,
			string workflow )
		{
			// Get the workflow assignment name.
			string assignmentName = "Assignment: " + objectName.Split( '.' )[ 0 ];

			// Get the assigned to user.
			string assignedToUser = this.mfContext.UsernameOfUser( "user2" );

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Set the user property value.
			mdCard.Properties.AddPropertyAndSetValue( propertyName, assignedToUser );

			// Set the workflow.
			mdCard.Workflows.SetWorkflow( workflow );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Expand the relationship tree of the object.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships = relationships.ExpandRelationships();

			// Assert that workflow assignment relationship header is displayed in listing.
			Assert.True( relationships.IsRelationshipHeaderInRelationships( WorkflowAssignmentRelationshipHeader ) );

			// Expand the workflow assignment relationship header.
			relationships = relationships.ExpandRelationshipHeader( WorkflowAssignmentRelationshipHeader );

			// Select the workflow assignment in the relationship tree.
			mdCard = relationships.SelectRelatedObject( WorkflowAssignmentRelationshipHeader, assignmentName );

			// Assert that workflow assignment is assigned to the user based on the metadatacard property.
			Assert.AreEqual( assignedToUser, mdCard.Properties.GetPropertyValue( AssignedToProperty ),
				"Mismatch between the expected and actual assigned to user in workflow assignment." );
		}

		/// <summary>
		/// Testing that automatic state transition is performed when workflow assignment is mark completed.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"ESTT Corporation (IT)",
			"Customer",
			"MarkComplete Workflow",
			"State2" )]
		public void AutomaticStateTransitionPerformedWhenSeparateAssignmentIsCompleted(
			string objectName,
			string objectType,
			string workflow,
			string expectedAutomaticStateTransition )
		{
			// Get the workflow assignment name.
			string assignmentName = "Assignment: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName );

			// Mark complete the assignment.
			mdCard.AssignmentOperations.MarkComplete( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Completed", assignmentName, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is performed.
			this.AssertAutomaticStateTransitionObject( expectedAutomaticStateTransition, initialObjectVersion + 1, ServerUserName, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "mark completed" ) );

		}

		/// <summary>
		/// Testing that automatic state transition is performed when workflow assignment is approved.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Austin District Redevelopment",
			"Project",
			"ApproveOrReject Workflow",
			"Approved" )]
		public void AutomaticStateTransitionPerformedWhenSeparateAssignmentIsApproved(
			string objectName,
			string objectType,
			string workflow,
			string expectedAutomaticStateTransition )
		{
			// Get the workflow assignment name.
			string assignmentName = "Assignment: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName );

			// Approve the assignment.
			mdCard.AssignmentOperations.Approve( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Approved", assignmentName, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is performed.
			this.AssertAutomaticStateTransitionObject( expectedAutomaticStateTransition, initialObjectVersion + 1, ServerUserName, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "approved" ) );
		}

		/// <summary>
		/// Testing that automatic state transition is performed when workflow assignment is rejected.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Samuel Lewis",
			"Employee",
			"ApproveOrReject Workflow",
			"Rejected" )]
		public void AutomaticStateTransitionPerformedWhenSeparateAssignmentIsRejected(
			string objectName,
			string objectType,
			string workflow,
			string expectedAutomaticStateTransition )
		{
			// Get the workflow assignment name.
			string assignmentName = "Assignment: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName );

			// Reject the assignment.
			mdCard.AssignmentOperations.Reject( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertRejectedAssignmentDetails( assignmentName, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is performed.
			this.AssertAutomaticStateTransitionObject( expectedAutomaticStateTransition, initialObjectVersion + 1, ServerUserName, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "rejected" ) );
		}

		/// <summary>
		/// Testing that automatic state transition is performed when all the workflow assignments are mark completed.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Sales Invoice 250 - Davis & Cobb, Attorneys at Law.xls",
			"Document",
			"MultiAssignment Complete",
			"State1",
			"State2" )]
		public void AutomaticStateTransitionPerformedWhenAllSeparateAssignmentsCompleted(
			string objectName,
			string objectType,
			string workflow,
			string initialWorkflowStateTransition,
			string expectedAutomaticStateTransition )
		{
			// Get the workflow assignments name.
			string assignmentName1 = "Assignment1: " + objectName.Split( '.' )[ 0 ];
			string assignmentName2 = "Assignment2: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName1 );

			// Mark complete the assignment.
			mdCard.AssignmentOperations.MarkComplete( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Completed", assignmentName1, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is not performed.
			Assert.AreEqual( initialWorkflowStateTransition, mdCard.Workflows.WorkflowState,
				"Mismatch between the expected and actual workflow state transition when one assignment only mark completed among multiple workflow assignments." );

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName2 );

			// Mark complete the assignment.
			mdCard.AssignmentOperations.MarkComplete( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Completed", assignmentName2, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is performed.
			this.AssertAutomaticStateTransitionObject( expectedAutomaticStateTransition, initialObjectVersion + 1, ServerUserName, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "mark completed" ) );
		}

		/// <summary>
		/// Testing that automatic state transition is performed when all the workflow assignments are approved.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Kimberley Miller",
			"Employee",
			"MultiAssignment ApproveOrReject",
			"State1",
			"State2" )]
		public void AutomaticStateTransitionPerformedWhenAllSeparateAssignmentsApproved(
			string objectName,
			string objectType,
			string workflow,
			string initialWorkflowStateTransition,
			string expectedAutomaticStateTransition )
		{
			// Get the workflow assignments name.
			string assignmentName1 = "Assignment1: " + objectName.Split( '.' )[ 0 ];
			string assignmentName2 = "Assignment2: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName1 );

			// Approve the assignment.
			mdCard.AssignmentOperations.Approve( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Approved", assignmentName1, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is not performed.
			Assert.AreEqual( initialWorkflowStateTransition, mdCard.Workflows.WorkflowState,
				"Mismatch between the expected and actual workflow state transition when one assignment only approved among multiple workflow assignments." );

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName2 );

			// Approve the assignment.
			mdCard.AssignmentOperations.Approve( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Approved", assignmentName2, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is performed.
			this.AssertAutomaticStateTransitionObject( expectedAutomaticStateTransition, initialObjectVersion + 1, ServerUserName, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "approved" ) );
		}

		/// <summary>
		/// Testing that automatic state transition is performed when any one of the workflow assignment is rejected.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Proposal 7722 - S&C Southwest Power",
			"Document collection",
			"MultiAssignment ApproveOrReject",
			"State1",
			"State3" )]
		public void AutomaticStateTransitionPerformedWhenAnyOneOfSeparateAssignmentIsRejected(
			string objectName,
			string objectType,
			string workflow,
			string initialWorkflowStateTransition,
			string expectedAutomaticStateTransition )
		{
			// Get the workflow assignments name.
			string assignmentName1 = "Assignment1: " + objectName.Split( '.' )[ 0 ];
			string assignmentName2 = "Assignment2: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName1 );

			// Reject the assignment.
			mdCard.AssignmentOperations.Reject( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertRejectedAssignmentDetails( assignmentName1, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is performed.
			this.AssertAutomaticStateTransitionObject( expectedAutomaticStateTransition, initialObjectVersion + 1, ServerUserName, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "rejected" ) );
		}

		/// <summary>
		/// Testing that automatic state transition is not performed when any one of the workflow assignment is not approved.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Office Design",
			"Project",
			"MultiAssignment Approve",
			"State1" )]
		public void AutomaticStateTransitionIsNotPerformedWhenAnyOneOfTheSeparateAssignmentIsNotApproved(
			string objectName,
			string objectType,
			string workflow,
			string initialWorkflowStateTransition )
		{
			// Get the workflow assignments name.
			string assignmentName1 = "Assignment1: " + objectName.Split( '.' )[ 0 ];
			string assignmentName2 = "Assignment2: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName1 );

			// Approve the assignment.
			mdCard.AssignmentOperations.Approve( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertCompletedOrApprovedAssignmentDetails( "Approved", assignmentName1, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is not performed.
			Assert.AreEqual( initialWorkflowStateTransition, mdCard.Workflows.WorkflowState,
				"Mismatch between the expected and actual workflow state transition when one assignment only approved among multiple workflow assignments." );

			// Select the workflow assignment in the relationship tree.
			mdCard = this.ExpandRelationshipAndSelectRelatedObject( homePage.ListView, objectName, assignmentName2 );

			// Reject the assignment.
			mdCard.AssignmentOperations.Reject( true, this.username );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert the assignment details.
			this.AssertRejectedAssignmentDetails( assignmentName2, mdCard );

			// Select the object.
			mdCard = this.SearchAndSelectObject( homePage, objectName, objectType );

			// Assert that automatic state transition is not performed.
			this.AssertAutomaticStateTransitionObject( initialWorkflowStateTransition, initialObjectVersion, username, mdCard,
				string.Format( AutomaticStateTransitionMismatch, "approved and rejected" ) );
		}

		/// <summary>
		/// Testing that workflow assignment is changed to normal assignment when manual state transition is performed
		/// before assignment complete.
		/// </summary>
		[Test]
		[Category( "WorkflowAssignments" )]
		[TestCase(
			"Jeff Smith",
			"Contact person",
			"SeparateAssignment Workflow",
			"State2" )]
		public void ManualStateTransitionChangesWorkflowAssignment(
			string objectName,
			string objectType,
			string workflow,
			string manualWorkflowStateTransition )
		{
			// Get the workflow assignment name.
			string assignmentName = "Assignment: " + objectName.Split( '.' )[ 0 ];

			// Launches the driver and starts the test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Set the workflow for the object.
			MetadataCardRightPane mdCard = this.SearchObjectAndSetWorkflow( homePage, objectName, objectType, workflow );

			// Expand the relationship tree of the object.
			RelationshipsTree relationships = homePage.ListView.GetRelationshipsTreeOfObject( objectName ).ExpandRelationships();

			// Assert that expected relationship header is displayed.
			Assert.True( relationships.IsRelationshipHeaderInRelationships( WorkflowAssignmentRelationshipHeader ),
				"Mismatch between the expected and actual relationship header in the selected object in list view." );

			// Perform the manual state transition.
			mdCard.Workflows.SetWorkflowStateTransition( manualWorkflowStateTransition );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that relationship header for workflow assignments is no longer displayed
			// because the assignment is no longer a worklfow assignment.
			Assert.False( relationships.IsRelationshipHeaderInRelationships( WorkflowAssignmentRelationshipHeader ),
				"Mismatch between the expected and actual relationship header presence in the selected object relationship tree in list view." );

			// Get the object version.
			int initialObjectVersion = mdCard.Header.Version;

			// The workflow assignment should be automatically updated by the server when the state
			// was changed. Go to home view and search for the object again to refresh the Assignment object
			// in the listing.
			homePage.TopPane.TabButtons.HomeTabClick();
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );
			
			// Expand the relationship tree of the object.
			relationships = listing.GetRelationshipsTreeOfObject( objectName ).ExpandRelationships();

			// Expand the assignment relationship header.
			relationships = relationships.ExpandRelationshipHeader( "Assignments" );

			// Select the assignment in the relationship tree.
			mdCard = relationships.SelectRelatedObject( "Assignments", assignmentName );

			// Assert that assigned to property is empty.
			Assert.IsEmpty( mdCard.Properties.GetPropertyValue( AssignedToProperty ), 
				"Mismatch between the expected and actual assigned to property value in the workflow assignment." );

		}
	}
}