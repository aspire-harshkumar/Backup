using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -10 )]
	[Category( "ExternalRepository" )]
	[Parallelizable( ParallelScope.Self )]
	class PinboardOperationsInExternalRepository : PinboardOperations
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected new readonly string classID;

		public PinboardOperationsInExternalRepository()
		{
			this.classID = "PinboardOperationsInExternalRepository";
		}

		[OneTimeSetUp]
		public override void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetTestUsers( 4 );

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "NFC_sample_vault.mfb", users );

			// Get the vault name.
			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user1" );
			this.password = this.mfContext.PasswordOfUser( "user1" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

			// Configure the Network Folder Connector in the vault.
			EnvironmentSetupHelper.ConfigureNetworkFolderConnectorToVault( this.mfContext, this.classID );

			// Promote objects in the vault.
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "sample_pdfa.pdf" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "article.aspx.txt" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "MultipleRecipientsInTOCC.msg" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "PowerPoint with M-Files Properties-2.pptx" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "exportdatabase2.vsd" );
		}

		[OneTimeTearDown]
		public void ExternalRepositoryCleanup()
		{
			EnvironmentSetupHelper.ClearExternalRepository( this.classID );
		}

		/// <summary>
		/// Pin an external object to pinboard. Then use pinboard to navigate to view the object. Finally, unpin the object
		/// from pinboard.
		/// </summary>
		[Test]
		[TestCase(
			"Minutes Project Meeting 4 2007-7.docx",
			"",
			"Minutes Project Meeting 4 2007-7",
			"PinboardOperationsInExternalRepository",
			Description = "Unmanaged Object." )]
		[TestCase(
			"MultipleRecipientsInTOCC.msg",
			"",
			"MultipleRecipientsInTOCC",
			Description = "Managed Object." )]
		public override void PinAndUnpinObject(
			string externalObjectName,
			string connectionName,
			string objectTitle,
			string breadCrumbItem = "" )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.PinAndUnpinObject( externalObjectName, connectionName, objectTitle, breadCrumbItem );
		}

		/// <summary>
		/// Pin an external view which is inside some external view to pinboard. Then use pinboard to navigate to the view. 
		/// Finally, unpin the view from pinboard.
		/// </summary>
		[TestCase(
			"",
			"SubFolder4",
			"SpecialCharacters.msg" )]
		public override void PinAndUnpinViewInsideAnotherView(
			string connectionName,
			string viewItem,
			string controlItem )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.PinAndUnpinViewInsideAnotherView( connectionName, viewItem, controlItem );
		}

		/// <summary>
		/// Pin an external view to pinboard. Then use pinboard to navigate to the view. 
		/// Finally, unpin the view from pinboard.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[TestCase( "", "Parrot.jpg" )]
		public override void PinAndUnpinViewInHomeView( string externalView, string controlItem )
		{
			// Get the external view name in local variable.
			externalView = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.PinAndUnpinViewInHomeView( externalView, controlItem );
		}

		/// <summary>
		/// Unpin an external object by opening the context menu directly in the pinboard.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[TestCase(
			"InReplyTo.msg",
			"",
			"InReplyTo",
			Description = "Unmanaged Object." )]
		[TestCase(
			"PowerPoint with M-Files Properties-2.pptx",
			"",
			"PowerPoint with M-Files Properties-2",
			Description = "Managed Object." )]
		public override void UnpinObjectFromPinboard(
			string externalObjectName,
			string connectionName,
			string objectTitle )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.UnpinObjectFromPinboard( externalObjectName, connectionName, objectTitle );
		}

		/// <summary>
		/// Add multiple external objects to pinboard and then navigate to them by using the pinboard.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[TestCase(
			"ocrtestmfd_small.tif;SpecialCharacters.eml;article.aspx.txt",
			"ocrtestmfd_small;SpecialCharacters;article.aspx",
			"PinboardOperationsInExternalRepository;PinboardOperationsInExternalRepository;PinboardOperationsInExternalRepository" )]
		public override void PinMultipleObjects(
			string externalObjects,
			string objectTitles,
			string objectTypes )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.PinMultipleObjects( externalObjects, objectTitles, objectTypes );
		}

		/// <summary>
		/// Add multiple external views/virtual folders to pinboard and then navigate to them by using pinboard.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[TestCase(
			"PinboardOperationsInExternalRepository;PinboardOperationsInExternalRepository",
			"SubFolder2;SubFolder3",
			"OrganizationOrg Chart.vstx;Asennusohje - M-Files laiterekisteri.docx" )]
		public override void PinMultipleViewsAndVirtualFolders(
			string viewPaths,
			string viewItems,
			string controlItems )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.PinMultipleViewsAndVirtualFolders( viewPaths, viewItems, controlItems );
		}

		/// <summary>
		/// Testing that the re-arranged positions of the pinned external object is retained.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[Order( 1 )]
		[TestCase(
			"",
			"Income Statement 10 2006-4.xlt;Minutes Project Meeting 4 2007-1.doc;Icons_102.png",
			"Icons_102",
			"Income Statement 10 2006-4" )]
		public override void CheckRearrangedPinnedObjectPositionIsRetained(
			string connectionName,
			string pinItems,
			string dragItem,
			string toItem )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.CheckRearrangedPinnedObjectPositionIsRetained( connectionName, pinItems, dragItem, toItem );
		}

		/// <summary>
		/// Testing that the re-arranged positions of the pinned external view is retained.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[Order( 1 )]
		[TestCase(
			"",
			"SubFolder1;SubFolder5;SubFolder6",
			"SubFolder1",
			"SubFolder5" )]
		public override void CheckRearrangedPinnedViewPositionIsRetained(
			string connectionName,
			string pinItems,
			string dragItem,
			string toItem )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.CheckRearrangedPinnedViewPositionIsRetained( connectionName, pinItems, dragItem, toItem );
		}

		// Overriding the test that are not relevant for external repositories at the moment and Overriding is done without Test attribute so NUnit will not execute them.
		public override void PinnedObjectWithFilesHasExpandedRelationships( string objectName, string objectType, string attachedFiles, string relatedObjects )
		{
			return;
		}

		// Overriding the test that requires more than 100 external objects and Overriding is done without Test attribute so NUnit will not execute them.
		public override void MaxNumberOfPinnedItems( string expectedMessage, string excessiveItem )
		{
			return;
		}
	}
}