using System.Collections.Generic;
using System.Linq;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.AssertHelpers;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -9 )]
	[Parallelizable( ParallelScope.Self )]
	class Workflows
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
		private readonly string additionalAssertMessageForWorkflows = "Mismatch between the expected and actual workflows.";
		private readonly string additionalAssertMessageForWorkflowStateTransitions = "Mismatch between the expected and actual workflow state transitions.";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public Workflows()
		{
			this.classID = "Workflows";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetDifferentTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Workflows Vault.mfb", users );

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
		/// Testing that the new object is created with workflow and state.
		/// </summary>
		[Test]
		[TestCase(
			"Document",
			"Microsoft PowerPoint Presentation (.pptx)",
			"Memo",
			"Name or title",
			"WorkflowInNewDocumentObject",
			"Processing job applications",
			"Job application received, awaiting review",
			Description = "Create new document object with workflow.", Category = "Smoke" )]
		[TestCase(
			"Customer",
			"",
			"Customer",
			"Customer name",
			"WorkflowInCustomerObject",
			"Contract Approval Workflow",
			"Draft",
			Description = "Create new customer object with workflow.", Category = "Smoke" )]
		public void AssignWorkflowInNewObject(
			string objectType,
			string template,
			string classValue,
			string nameProperty,
			string objectName,
			string workflow,
			string workflowState )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open the new object metadatacard.
			MetadataCardPopout newObjMDCard = null;
			if( objectType.Equals( "Document" ) )
			{
				// Open the new object metadata card from the template selector.
				TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

				// Filter to see blank templates and select the template.
				templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );
				templateSelector.SelectTemplate( template );

				// Click next button to proceed to the metadata card of the new object.
				newObjMDCard = templateSelector.NextButtonClick();

				// Click check-in immediately check box for document object.
				newObjMDCard.CheckInImmediatelyClick();
			}
			else
				newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Set class and name.
			newObjMDCard.Properties.SetPropertyValue( "Class", classValue );
			newObjMDCard.Properties.SetPropertyValue( nameProperty, objectName );

			// Set the workflow.
			newObjMDCard.Workflows.SetWorkflow( workflow );

			// Set the workflow state.
			newObjMDCard.Workflows.SetWorkflowStateTransition( workflowState );

			// Click create button and get the rightpane metadata card of the new object.
			MetadataCardRightPane mdCard = newObjMDCard.SaveAndDiscardOperations.Save();

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, workflowState, mdCard.Workflows );

		} // end AssignWorkflowInNewObject

		/// <summary>
		/// Testing that the workflow and state is set in the metadatacard of existing object.
		/// </summary>
		[Test]
		[TestCase(
			"WorkflowInDocumentCollectionObject",
			"Document collection",
			"Contract Approval Workflow",
			"",
			"(no transition)",
			",Contract Approval Workflow,Processing job applications",
			"(no transition);Draft",
			Description = "Add Workflow with no state in existing object", Category = "Smoke" )]
		[TestCase(
			"WorkflowInAssignmentObject",
			"Assignment",
			"Processing job applications",
			"Job application received, awaiting review",
			"Job application received, awaiting review",
			",Contract Approval Workflow,Processing job applications",
			"(no transition);Applicant not selected;Applicant proposed for hiring;Applicant selected for interview;Applicant withdrew;Interview held;Interview scheduled;Job application received, awaiting review",
			Description = "Add Workflow with initial state in existing object", Category = "Smoke" )]
		public void AssignWorkflowInExistingObject(
			string objectName,
			string objectType,
			string workflow,
			string workflowState,
			string workflowStateTransition,
			string workflows,
			string stateTransitions )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Set the workflow.
			mdCard.Workflows.SetWorkflow( workflow );

			// Check the available workflows.
			Assert.AreEqual( workflows.Split( ',' ).ToList(), mdCard.Workflows.GetAvailableWorkflows(), additionalAssertMessageForWorkflows );

			// Set the workflow state.
			mdCard.Workflows.SetWorkflowStateTransition( workflowStateTransition );

			// Check the available workflows.
			Assert.AreEqual( stateTransitions.Split( ';' ).ToList(), mdCard.Workflows.GetAvailableStateTransitions(), additionalAssertMessageForWorkflowStateTransitions );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, workflowState, mdCard.Workflows );

			// Check if object version is increased after set the workflow and state.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );

		} // end AssignWorkflowInExistingObject

		/// <summary>
		/// Testing that the default workflow and state is set in the metadatacard for the selected class in new object.
		/// </summary>
		[Test]
		[TestCase(
			"Document",
			"AutoCAD Drawing Template (ISO A1).dwg",
			"Drawing",
			"Name or title",
			"DefaultWorkflowInNewDocumentObject",
			"Reviewing drawings",
			"Listed for approval",
			",Contract Approval Workflow,Processing job applications,Reviewing drawings",
			"(no transition),Approved,Listed for approval",
			Description = "Create new document object with Default workflow.", Category = "Smoke" )]
		public void DefaultWorkflow(
			string objectType,
			string template,
			string className,
			string nameProperty,
			string objectName,
			string workflow,
			string workflowState,
			string expectedWorkflows,
			string expectedStateTransitions )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Start create new object.
			MetadataCardPopout newObjMDCard = null;

			if( !template.Equals( "" ) )  // Checks if template is non empty.
			{
				// Open the new object metadata card from the template selector.
				TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

				// Filter to see blank templates and select the template.
				templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.All );
				templateSelector.SelectTemplate( template );

				// Click next button to proceed to the metadata card of the new object.
				newObjMDCard = templateSelector.NextButtonClick();

				// Check the class is updated in the metadatacard based on the selected template.
				Assert.AreEqual( className, newObjMDCard.Properties.GetPropertyValue( "Class" ) );

			}
			else
				newObjMDCard = homePage.TopPane.CreateNewObject( objectType );  // New object metadata card without template.

			// Set name.
			newObjMDCard.Properties.SetPropertyValue( nameProperty, objectName );

			// Verify if the metadata card is set with the default workflow and state in the metadatacard.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, workflowState, newObjMDCard.Workflows );

			// Gets the workflow values from the workflow value list.
			List<string> actualAvailableWorkflows = newObjMDCard.Workflows.GetAvailableWorkflows();

			// Check all the workflows displayed with Default workflow.
			Assert.AreEqual( expectedWorkflows.Split( ',' ).ToList(), actualAvailableWorkflows, additionalAssertMessageForWorkflows );

			// Gets the workflow values from the state transitions value list.
			List<string> actualAvailableStateTransitions = newObjMDCard.Workflows.GetAvailableStateTransitions();

			// Check all the possible state transitions displayed.
			Assert.AreEqual( expectedStateTransitions.Split( ',' ).ToList(), actualAvailableStateTransitions, additionalAssertMessageForWorkflowStateTransitions );

			// Click check-in immediately check box for document object.
			newObjMDCard.CheckInImmediatelyClick();

			// Click create button and the metadata card of the new object appears to the right pane.
			MetadataCardRightPane mdCard = newObjMDCard.SaveAndDiscardOperations.Save();

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, workflowState, mdCard.Workflows );

		} // end DefaultWorkflow

		/// <summary>
		/// Testing that the forced default workflow and state is set in the metadatacard for the selected class in existing object.
		/// </summary>
		[Test]
		[TestCase(
			"Document",
			"Project Plan / Records Management.doc",
			"Purchase Invoice",
			"Reviewing and accepting purchase invoices",
			"Received, awaiting checking",
			"(no transition);Received, awaiting checking",
			Description = "Check Forced Default workflow is set when selecting the class in the existing object.", Category = "Smoke" )]
		public void ForcedDefaultWorkflow(
			string objectType,
			string objectName,
			string className,
			string workflow,
			string stateTransition,
			string expectedStateTransitions )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Set class which have forced default workflow.
			mdCard.Properties.SetPropertyValue( "Class", className );

			// Wait until the Forced workflow is set in the metadatacard.
			mdCard.Workflows = mdCard.Workflows.WaitUntilWorkflowStateTransitionIconDisplayed();

			// Verify if the metadata card is set with the default workflow and state in the metadatacard.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, "(no state)" + stateTransition, mdCard.Workflows );

			// Assign the expected workflow value in list.
			List<string> expectedWorkflows = new List<string> { "", workflow };

			// Gets the workflow values from the workflow value list.
			List<string> actualAvailableWorkflows = mdCard.Workflows.GetAvailableWorkflows();

			// Check the forced workflow alone displayed.
			Assert.AreEqual( expectedWorkflows, actualAvailableWorkflows, additionalAssertMessageForWorkflows );

			// Gets the workflow values from the state transitions value list.
			List<string> actualAvailableStateTransitions = mdCard.Workflows.GetAvailableStateTransitions();

			// Check all the possible state transitions displayed.
			Assert.AreEqual( expectedStateTransitions.Split( ';' ).ToList(), actualAvailableStateTransitions, additionalAssertMessageForWorkflowStateTransitions );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, stateTransition, mdCard.Workflows );

		} // end ForcedDefaultWorkflow


		/// <summary>
		/// Testing that the new object is not created without class mandatory workflow and warning message is displayed.
		/// </summary>
		[Test]
		[TestCase(
			"Document",
			"Document Template for Word.doc",
			"Automation Test",
			"Name or title",
			"ObjectWithoutClassMandatoryWorkflow",
			"Automation Test",
			"Draft",
			Description = "Check new object is not created without class mandatory workflow and warning message is displayed.", Category = "Smoke" )]
		public void RemoveClassMandatedWorkflow(
			string objectType,
			string template,
			string className,
			string nameProperty,
			string objectName,
			string workflow,
			string workflowState )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open the new object metadata card from the template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( objectType );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.All );
			templateSelector.SelectTemplate( template );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout newObjMDCard = newObjMDCard = templateSelector.NextButtonClick();

			// Set class and name.
			newObjMDCard.Properties.SetPropertyValue( "Class", className );
			newObjMDCard.Properties.SetPropertyValue( nameProperty, objectName );

			// Verify if the metadata card is set with the default workflow and state in the metadatacard.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, workflowState, newObjMDCard.Workflows );

			// Set the empty value for the workflow.
			newObjMDCard.Workflows.RemoveWorkflow();

			// Click check-in immediately check box for document object.
			newObjMDCard.CheckInImmediatelyClick();

			// Click create button and the metadata card of the new object appears to the right pane.
			MessageBoxDialog messageBoxDialog = newObjMDCard.SaveAndDiscardOperations.SaveAndWaitForMessageBoxDialog();

			// Check expected warning message is displayed in message dialog.
			Assert.AreEqual( "The field \"Workflow\" must not be empty.", messageBoxDialog.Message );

			// Close the message dialog.
			messageBoxDialog.OKClick();

			// Close the metadatacard.
			newObjMDCard.DiscardChanges();

		} // end RemoveClassMandatedWorkflow

		/// <summary>
		/// Testing that the state transition in checked out existing object.
		/// </summary>
		[Test]
		[TestCase(
			"WorkflowStateTransitionInCheckedOutObj",
			"Document",
			"Reviewing drawings",
			"Approved",
			Description = "Perform state transition in checkedout MFD object.", Category = "Smoke" )]
		[TestCase(
			"WorkflowStateTransitionInCheckedoutNonDocObj",
			"Contact person",
			"Processing job applications",
			"Interview scheduled",
			Description = "Perform state transition in checkedout non document object.", Category = "Smoke" )]
		public void StateTransitionInCheckedOutObject(
			string objectName,
			string objectType,
			string workflow,
			string workflowStateTransition )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Checkout the object.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Open the pop out metadata card.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Get the initial workflow state.
			string initialWorkflowState = mdCardPopOut.Workflows.WorkflowState;

			// Set the workflow state.
			mdCardPopOut.Workflows.SetWorkflowStateTransition( workflowStateTransition );

			// Save the changes.
			mdCard = mdCardPopOut.SaveAndDiscardOperations.Save();

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, initialWorkflowState + workflowStateTransition, mdCard.Workflows );

			// Check in the object.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, workflowStateTransition, mdCard.Workflows );

		} // end StateTransitionInCheckedOutObject

		/// <summary>
		/// Testing that the state transition with action in existing object.
		/// </summary>
		[Test]
		[TestCase(
			"WorkflowStateTransitionWithActionInDocxFileType.docx",
			"Document",
			"Contract Approval Workflow",
			"Draft,Waiting for Approval,Approved",
			"Assignment description",
			"A new contract is awaiting approval.",
			"Deadline",
			Description = "Perform state transition with action to the document object with extension .docx", Category = "Smoke" )]
		[TestCase(
			"WorkflowStateTransitionWithActionInTxtFileType.txt",
			"Document",
			"Contract Approval Workflow",
			"Draft,Waiting for Approval,Approved",
			"Assignment description",
			"A new contract is awaiting approval.",
			"Deadline",
			Description = "Perform state transition with action to the document object with extension .txt", Category = "Smoke" )]
		[TestCase(
			"WorkflowStateTransitionWithActionInNonDocumentObject",
			"Project",
			"Contract Approval Workflow",
			"Draft,Waiting for Approval,Approved",
			"Assignment description",
			"A new contract is awaiting approval.",
			"Deadline",
			Description = "Perform state transition with action to the non-document object", Category = "Smoke" )]
		public void WorkflowStateTransitionWithActions(
			string objectName,
			string objectType,
			string workflow,
			string workflowStates,
			string assignmentProperty,
			string assignmentPropertyValue,
			string deadlineProperty )
		{

			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the object name without extension from the object name.
			string objName = objectName.Split( '.' )[ 0 ];

			// Get the number of state transitions need to be performed.
			string[] stateTransitions = workflowStates.Split( ',' );

			// Set the workflow for the object.
			mdCard.Workflows.SetWorkflow( workflow );

			// Perform the state transition based on the number of states available.
			foreach( string stateTransition in stateTransitions )
			{
				// Set the workflow state.
				mdCard.Workflows.SetWorkflowStateTransition( stateTransition );

				// Save the changes.
				mdCard = mdCard.SaveAndDiscardOperations.Save();

			} // end foreach

			// Verify the workflow and state value is set as expected in the right pane metadata card.
			MetadataCardAssertHelper.AssertMDCardWorkflowTableInfo( workflow, stateTransitions[ stateTransitions.Length - 1 ], mdCard.Workflows );

			// Verify the property values which will be updated after the state transition.
			Assert.AreEqual( assignmentPropertyValue, mdCard.Properties.GetPropertyValue( assignmentProperty ) );
			Assert.AreEqual( TimeHelper.GetModifiedDate( 0, 3, 0 ), mdCard.Properties.GetPropertyValue( deadlineProperty ) );

			// Verify the object is converted to pdf and extension of the object is changed for Document object.
			if( objectType.Equals( "Document" ) )
				Assert.IsTrue( listing.IsItemInListing( objName + ".pdf" ) );
			else
				Assert.IsTrue( listing.IsItemInListing( objectName ) );

		} // end WorkflowStateTransitionWithActions

	} // end Workflows
}