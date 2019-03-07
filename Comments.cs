using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -8 )]
	[Parallelizable( ParallelScope.Self )]
	public class Comments
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

		public Comments()
		{
			this.classID = "Comments";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetDifferentTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Data Types And Test Objects.mfb", users );

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
		/// Testing that the comments is successfully added in the metadata card and object version is increased.
		/// </summary>
		[Test]
		[TestCase(
			"AddCommentsInDocumentObjectType.doc",
			"Document",
			"Sample comments content. \"045fc3c2-369f-4f13-9a50-5bed5e23ff98\", \"0123456789\", \"!@#$%\"",
			Description = "Document object without any comments yet.", Category = "Smoke" )]
		[TestCase(
			"AddCommentsInNonDocumentObjectType",
			"Contact person",
			"Comment with special character (or) symbol in Key field [For eg: !@#$%^&*()_+{}|:\" <>?[]\\;',./], <a href=\"https://www.m-files.com\">M-Files</a>",
			Description = "Non-Document object with existing comments.", Category = "Smoke" )]
		public void AddCommentInMetadataCard(
			string objectName,
			string objectType,
			string comment )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;
			int initialCommentsCount = mdCard.Header.CommentsCount;

			// Open the comment section in the right pane metadatacard.
			mdCard.Comments = mdCard.Header.GoToComments();

			// Set the comment in the metadatacard.
			mdCard.Comments.SetComments( comment );

			// Forms the comment user info for verification and then save the comment.
			string expectedUserInfo = this.username + " " + TimeHelper.GetCurrentTime();
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Verify the comments count in header of the metadatacard.
			Assert.AreEqual( initialCommentsCount + 1, mdCard.Header.CommentsCount );

			// Get the latest comment.
			mdCard.Comments = mdCard.Header.GoToComments();
			string latestComment = mdCard.Comments.LatestComment;

			// Verify the comment is added in the metadatacard.
			Assert.AreEqual( comment, latestComment );

			// Get the latest comment user info.
			string latestCommentUserInfo = mdCard.Comments.UserAndDateOfLatestComment;

			// Verify the user information is updated correctly in the added comment.
			Assert.AreEqual( expectedUserInfo, latestCommentUserInfo );

			// Verify the object version is increased.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );

		} // end AddCommentInMetadataCard

		/// <summary>
		/// Testing that the comment is added successfully in the checked out object.
		/// </summary>
		[Test]
		[TestCase(
			"AddCommentInChecked_outDocumentObject.xls",
			"Document",
			"Sample checked out object comment. \"no-reply@motivesys.com\" or \"m-files%321@m-files.com\"",
			Description = "Document object with existing comment.", Category = "Smoke" )]
		[TestCase(
			"AddCommentInChecked_outNon_DocumentObject",
			"Employee",
			"Numerics: 0.0000098, 1299999999999.351, *4654564564569***132132",
			Description = "Non-document object without any comments yet.", Category = "Smoke" )]
		public void AddCommentInCheckedOutObject(
			string objectName,
			string objectType,
			string comment )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Gets the version of the object before checkout.
			int initialVersionBeforeCheckout = mdCard.Header.Version;
			int initialCommentsCount = mdCard.Header.CommentsCount;

			// Checkout the object.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Open the pop out metadata card.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Open the comment section in the right pane metadatacard.
			mdCardPopOut.Comments = mdCardPopOut.Header.GoToComments();

			// Set the comment in the metadatacard.
			mdCardPopOut.Comments.SetComments( comment );

			// Save the comment.
			mdCard = mdCardPopOut.SaveAndDiscardOperations.Save();

			// Check the comments count in checked out object rightpane metadata card.
			Assert.AreEqual( initialCommentsCount + 1, mdCard.Header.CommentsCount );

			// Get the entered comment in checked out object rightpane metadata card.
			mdCard.Comments = mdCard.Header.GoToComments();
			string latestComment = mdCard.Comments.EnteredCommentInTextBox;

			// Check the entered comment is displayed in the comments text box.
			Assert.AreEqual( comment, latestComment );

			// Check in the object.
			string expectedUserInfo = this.username + " " + TimeHelper.GetCurrentTime();  // Before check-in forms the comment user info for verification.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Open the pop out metadata card.
			mdCardPopOut = mdCard.PopoutMetadataCard();

			// Verify the comments count is increased in header of the metadatacard.
			Assert.AreEqual( initialCommentsCount + 1, mdCardPopOut.Header.CommentsCount );

			// Get the latest comment.
			mdCardPopOut.Comments = mdCardPopOut.Header.GoToComments();
			latestComment = mdCardPopOut.Comments.LatestComment;

			// Verify the comment is added in the metadatacard.
			Assert.AreEqual( comment, latestComment );

			// Get the latest comment user info.
			string latestCommentUserInfo = mdCardPopOut.Comments.UserAndDateOfLatestComment;

			// Verify the user information is updated correctly in the added comment.
			Assert.AreEqual( expectedUserInfo, latestCommentUserInfo );

			// Verify the object version is increased after check-in the object.
			Assert.AreEqual( initialVersionBeforeCheckout + 1, mdCardPopOut.Header.Version );

			// Close the popout metadatacard.
			mdCardPopOut.CloseButtonClick();

		} // end AddCommentInCheckedOutObject

		/// <summary>
		/// Check that comments cannot be added as read-only user. The comments text box is not editable.
		/// </summary>
		[Test]
		[TestCase(
			"readonly",
			"AddCommentsInDocumentObjectType.doc",
			"Document",
			Description = "Document object type.", Category = "Smoke" )]
		[TestCase(
			"readonly",
			"AddCommentsInNonDocumentObjectType",
			"Contact person",
			Description = "Non-Document object type.", Category = "Smoke" )]
		public void AddCommentIsNotPossibleForReadOnlyUser(
			string userNameTestData,
			string objectName,
			string objectType )
		{
			// Start the test in fresh driver with the mentioned user login credentials.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage( this.mfContext.UsernameOfUser( userNameTestData ), this.mfContext.PasswordOfUser( userNameTestData ), this.vaultName );

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Open the comment section in the right pane metadatacard.
			mdCard.Comments = mdCard.Header.GoToComments();

			// Verify if comments text box is not editable for the read only user.
			Assert.False( mdCard.Comments.CheckCommentsTextBoxFieldIsEditable() );

			//Quits the driver.
			this.browserManager.EnsureQuitBrowser();

		} // end AddCommentIsNotPossibleForReadOnlyUser


		/// <summary>
		/// Testing that the comments is not added the metadatacard when comment is discarded.
		/// </summary>
		[Test]
		[TestCase( "DiscardCommentsInDocumentObjectType.dwg", "Document",
			Description = "Document object.", Category = "Smoke" )]
		[TestCase( "DiscardCommentsInNonDocumentObjectType", "Project",
			Description = "Non-document object.", Category = "Smoke" )]
		public void DiscardComments( string objectName, string objectType )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version and comments count.
			int initialObjVersion = mdCard.Header.Version;
			int initialCommentsCount = mdCard.Header.CommentsCount;

			// Form the comment to be entered.
			string comment = GetCurrentMethodName() + "_" + TimeHelper.GetCurrentTime();

			// Open the comment section in the right pane metadatacard.
			mdCard.Comments = mdCard.Header.GoToComments();

			// Set the comment in the metadatacard.
			mdCard.Comments.SetComments( comment );

			// Discard the comment.
			mdCard = mdCard.DiscardChanges();

			// Verify the comments count is not increased in header of the metadatacard after discard the changes.
			Assert.AreEqual( initialCommentsCount, mdCard.Header.CommentsCount );

			// Get the latest comment.
			mdCard.Comments = mdCard.Header.GoToComments();
			string latestComment = mdCard.Comments.LatestComment;

			// Verify the comment is not added in the metadatacard after discard the changes.
			Assert.AreNotEqual( comment, latestComment );

			// Verify the object version is not increased after discard the changes.
			Assert.AreEqual( initialObjVersion, mdCard.Header.Version );

		} // end DiscardComments

		/// <summary>
		/// Testing that the comment is discarded successfully after undo checkout the object.
		/// </summary>
		[Test]
		[TestCase(
			"DiscardCommentsByUndoCheckout_Obj.txt",
			"Document",
			"Sample checked out object comment. \"no-reply@motivesys.com\" or \"m-files%321@m-files.com\"",
			Description = "Undo checkout document object.", Category = "Smoke" )]
		[TestCase(
			"DiscardCommentsByUndoCheckout_Non-DocObj",
			"Project",
			"Numerics: 0.0000098, 1299999999999.351, *4654564564569***132132",
			Description = "Undo checkout the non-document object.", Category = "Smoke" )]
		public void DiscardCommentByUndoCheckout(
			string objectName,
			string objectType,
			string comment )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Selects the object in the listing.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the version of the object before checkout.
			int initialVersionBeforeCheckout = mdCard.Header.Version;
			int initialCommentsCount = mdCard.Header.CommentsCount;

			// Checkout the object.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Open the pop out metadata card.
			MetadataCardPopout mdCardPopOut = mdCard.PopoutMetadataCard();

			// Open the comment section in the right pane metadatacard.
			mdCardPopOut.Comments = mdCardPopOut.Header.GoToComments();

			// Set the comment in the metadatacard.
			mdCardPopOut.Comments.SetComments( comment );

			// Save the comment.
			mdCard = mdCardPopOut.SaveAndDiscardOperations.Save();

			// Undo Checkout the object.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).UndoCheckOutObject();

			// Check the comments count is not increased after undo checkout in the rightpane metadata card.
			Assert.AreEqual( initialCommentsCount, mdCard.Header.CommentsCount );

			// Get the latest comment in rightpane metadata card.
			mdCard.Comments = mdCard.Header.GoToComments();
			string latestComment = mdCard.Comments.LatestComment;

			// Check the entered comment is discarded and not added in the right pane metadatacard
			Assert.AreNotEqual( comment, latestComment );

			// Navigate back to the properties pane in metadatacard.
			mdCard.Header.GoToProperties();

			// Open the pop out metadata card.
			mdCardPopOut = mdCard.PopoutMetadataCard();

			// Verify the comments count is not increased in header of the metadatacard after undocheckout.
			Assert.AreEqual( initialCommentsCount, mdCardPopOut.Header.CommentsCount );

			// Get the latest comment.
			mdCardPopOut.Comments = mdCardPopOut.Header.GoToComments();
			latestComment = mdCardPopOut.Comments.LatestComment;

			// Verify the comment is not added in the metadatacard.
			Assert.AreNotEqual( comment, latestComment );

			// Verify the object version is not increased after undo checkout.
			Assert.AreEqual( initialVersionBeforeCheckout, mdCard.Header.Version );

			// Closes the metadata card.
			mdCardPopOut.CloseButtonClick();

		} // end DiscardCommentByUndoCheckout

	}// end comments

}