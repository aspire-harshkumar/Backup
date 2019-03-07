using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -4 )]
	[Category( "ExternalRepository" )]
	[Parallelizable( ParallelScope.Self )]
	class PreviewExternalRepositoryFileContents : PreviewFileContents
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected new readonly string classID;

		public PreviewExternalRepositoryFileContents()
		{
			this.classID = "PreviewExternalRepositoryFileContents";
		}

		[OneTimeSetUp]
		public override void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "NFC_sample_vault.mfb", users );
			
			// Assign the vault name in local variable.
			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

			// Configure the Network Folder Connector in the vault.
			EnvironmentSetupHelper.ConfigureNetworkFolderConnectorToVault( this.mfContext, this.classID );

			// Promote objects in the vault.
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "sample_pdfa.pdf" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "article.aspx.txt" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "MultipleRecipientsInTOCC.msg" );
		}

		[OneTimeTearDown]
		public void ExternalRepositoryCleanup()
		{
			EnvironmentSetupHelper.ClearExternalRepository( this.classID );
		}

		/// <summary>
		/// Select an promoted object and switch from metadata tab to preview tab. Verify the 
		/// content is displayed in preview.
		/// </summary>
		[Test]
		[Category( "Preview" )]
		[TestCase( "sample_pdfa.pdf", 1 )]
		[TestCase( "article.aspx.txt", 7 )]
		[TestCase( "MultipleRecipientsInTOCC.msg", 1 )]
		public override void PreviewDocument( string objName, int expectedPageCount )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.PreviewDocument( objName, expectedPageCount );
		}

		/// <summary>
		/// Testing that the preview is not available for the unmanaged object.
		/// </summary>
		[Test]
		[Category( "Preview" )]
		[TestCase( "Word file with M-Files Properties.doc" )]
		[TestCase( "InReplyTo.msg" )]
		[TestCase( "M-Files HR -myyntiesitys-2.pptx" )]
		public override void NoPreviewForUnsupportedFileType( string objName, string connectionName = "" )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.NoPreviewForUnsupportedFileType( objName, connectionName );
		}

		/// <summary>
		/// Select unmanaged folder object and switch from metadata tab to preview tab.
		/// No preview should be shown for non-document object.
		/// </summary>
		[Test]
		[Category( "Preview" )]
		[TestCase( "SubFolder1" )]
		[TestCase( "SubFolder2" )]
		public override void NoPreviewForNonDocumentObject( string objName, string connectionName = "" )
		{
			// Get the connection name in local variable.
			connectionName = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.NoPreviewForNonDocumentObject( objName, connectionName );
		}

		/// <summary>
		/// Switch to preview tab and then select different managed objects to view their previews.
		/// Verify that expected number of content pages are displayed in preview.
		/// </summary>
		[Test]
		[Category( "Preview" )]
		[TestCase( "sample_pdfa.pdf;article.aspx.txt;MultipleRecipientsInTOCC.msg", "1;7;1" )]
		public override void PreviewSeveralDocumentsInRow( string objNames, string pageCounts, string searchWord = "" )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.PreviewSeveralDocumentsInRow( objNames, pageCounts, searchWord );
		}

		// Overriding the test that are not relevant for external repositories at the moment and Overriding is done without Test attribute so NUnit will not execute them.
		public override void PreviewAttachedFile( string objectName, string attachedFileName, int expectedPageCount )
		{
			return;
		}

		public override void PreviewAndMetadataTabsSwitchSeveralDocumentsInRow( string objects, string pageCounts, string view )
		{
			return;
		}

		public override void PreviewAndMetadataTabsAutomaticSwitchSeveralObjectsInRow( string doc1, string obj1, string doc2, string obj2, string searchWord, string objectType, string objectTypeHeader )
		{
			return;
		}

		public override void PreviewDocumentBySelectingAnnotationObject( string documentName, string attachedFileName, int expectedPageCount )
		{
			return;
		}
	}
}