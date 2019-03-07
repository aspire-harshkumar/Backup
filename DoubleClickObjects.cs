using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.PageObjects;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Motive.MFiles.vNextUI.PageObjects.Listing;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -5 )]
	[Parallelizable( ParallelScope.Self )]
	class DoubleClickObjects
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

		public DoubleClickObjects()
		{
			this.classID = "DoubleClickObjects";
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
		/// Double-clicking an SFD object to check it out, editing its properties and checking it in.
		/// </summary>
		/// <param name="objectName">Name of the SFD that will be checked out.</param>
		[TestCase( "Preview text.txt", "Keywords", "(ﾉಥ益ಥ）ﾉ﻿ ┻━┻" )]
		public void DoubleClickCheckOutSFDObjectModifyPropertiesCheckIn(
			string objectName,
			string property,
			string newPropertyValue )
		{
			// Search for the Document and save it's version number.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( objectName );
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			int previousVersion = mdCard.Header.Version;

			// Double-click check out.
			listing.DoubleClickDocument( objectName ).WaitUntilLoaded().CheckOutClick();

			// Assert that the document is checked out.
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser,
					listing.GetObjectCheckOutStatus( objectName ), objectName + " was not checked out" );

			// Change the properties
			mdCard.Properties.SetPropertyValue( property, newPropertyValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Check in.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Assert changed values.
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( newPropertyValue, mdCard.Properties.GetPropertyValue( property ) );
			Assert.AreEqual( previousVersion + 1, mdCard.Header.Version );
		}

		/// <summary>
		/// Double-clicks an SFD and cancels the check out.
		/// </summary>
		/// <param name="mfdObjectName"></param>
		[TestCase( "Door Chart 51E", "Top view.dwg" )]
		public void DoubleClickCancelCheckOutMFDObject( string mfdObjectName, string attachedFile )
		{
			// Search for the Document.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( mfdObjectName );

			// Double-click to open the dialog.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( mfdObjectName );
			relationships.ExpandRelationships();
			CheckOutDialog dialog = relationships.DoubleClickAttachedFile( attachedFile );

			// Assert that the right buttons are shown.
			ICollection<string> buttons = dialog.Buttons;
			Assert.AreEqual( 3, buttons.Count );

			// Note that the text content in buttons is "Check Out" etc but it is made 
			// upper-case by CSS. This causes some browsers to read it in all capital 
			// letters and some browsers read it as it is in the element without CSS. 
			// For this reason, accepting both options.
			Assert.That( buttons.Contains( "CHECK OUT" ) || buttons.Contains( "Check Out" ),
				"Check Out button not found" );
			Assert.That( buttons.Contains( "READ-ONLY" ) || buttons.Contains( "Read-Only" ),
				"Read-Only button not found" );
			Assert.That( buttons.Contains( "CANCEL" ) || buttons.Contains( "Cancel" ),
				"Cancel button not found" );

			// Cancel the check out.
			dialog.CancelClick();

			// Assert that the document was not checked out.
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( mfdObjectName ) );
		}

		/// <summary>
		/// Download an SFD document in read-only mode by double-clicking it.
		/// </summary>
		/// <param name="objectName">Name of the SFD that will be downloaded.</param>
		[TestCase( "Tennessee Land Surveyors Price List 2006.xls" )]
		public void DoubleClickReadOnlySFDObject( string objectName )
		{
			// Search for the Document.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( objectName );

			// The version number should stay the same.
			int expectedVersion = listing.SelectObject( objectName ).Header.Version;

			// Open read-only.
			listing.DoubleClickDocument( objectName ).WaitUntilLoaded().ReadOnlyClick();
			MetadataCardRightPane mdCard = home.MetadataCardPane;

			// Assert that the document is not checked out.
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( expectedVersion, mdCard.Header.Version );
		}

		/// <summary>
		/// Double-click MFD file to check the object out.
		/// </summary>
		/// <param name="parentName">MFD name.</param>
		/// <param name="childName">File name.</param>
		[TestCase( "Bill of Materials: Furniture", "Room #103.xls" )]
		public void DoubleClickCheckOutMFDObject( string parentName, string childName )
		{
			// Search for the parent object.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( parentName );
			RelationshipsTree relations = listing.GetRelationshipsTreeOfObject( parentName );

			// Double-click the child to check the MFD out.
			relations.ExpandRelationships();
			relations.DoubleClickAttachedFile( childName ).CheckOutClick();

			// Access the preview pane. First wait to make sure that preview has
			// finished loading after checking out the object.
			PreviewPane preview = home.PreviewPane.WaitUntilLoaded();

			// Assert that the MFD was checked out.
			Assert.AreEqual(
					CheckOutStatus.CheckedOutToCurrentUser, listing.GetObjectCheckOutStatus( parentName ) );

			// Assert that a preview is shown.
			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, preview.Status );

			// Assert that all the files of the MFD show as checked out.
			List<string> files = relations.AttachedFileNames.ToList();
			foreach( string file in files )
			{
				Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser,
						relations.GetAttachedFileCheckedOutStatus( file ) );
			}
		}

		/// <summary>
		/// Double-click MFD file to download it without checking the object out.
		/// </summary>
		/// <param name="parentName">MFD name.</param>
		/// <param name="childName">File name.</param>
		[TestCase( "Project Plan", "Project Plan.pdf" )]
		public void DoubleClickReadOnlyMFDObject( string parentName, string childName )
		{
			// Search for the parent object.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( parentName );
			RelationshipsTree relations = listing.GetRelationshipsTreeOfObject( parentName );

			// Double-click the child to download it in Read-Only mode.
			relations.ExpandRelationships();
			relations.DoubleClickAttachedFile( childName ).WaitUntilLoaded().ReadOnlyClick();
			PreviewPane preview = home.PreviewPane;

			// Assert that the MFD was not checked out.
			Assert.AreEqual(
					CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( parentName ) );

			// Assert that a preview is shown.
			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, preview.Status );

			// Assert that all the files of the MFD show as checked in.
			List<string> files = relations.AttachedFileNames.ToList();
			foreach( string file in files )
			{
				Assert.AreEqual( CheckOutStatus.CheckedIn, relations.GetAttachedFileCheckedOutStatus( file ) );
			}
		}

		/// <summary>
		/// Double-click an SFD to check it out and verify that it is in Recently accessed view.
		/// </summary>
		/// <param name="objectName">Name of the SFD.</param>
		[TestCase( "West Elevation.dwg" )]
		public void CheckedOutObjectIsInRecentlyAccessedView( string objectName )
		{
			// Search for the SFD.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( objectName );

			// Double-click the document.
			listing.DoubleClickDocument( objectName ).WaitUntilLoaded().CheckOutClick();

			// Open the recently accessed view.
			listing = home.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Recent );

			// Assert that the document is in the listing.
			Assert.That( listing.IsItemInListing( objectName ) );
			Assert.AreEqual(
					CheckOutStatus.CheckedOutToCurrentUser, listing.GetObjectCheckOutStatus( objectName ) );
		}

		/// <summary>
		/// Double-click an SFD object that was checked out by another user.
		/// </summary>
		/// <param name="objectName">Name of the SDF object.</param>
		[TestCase( "Preview presentation.pptx" )]
		public void ObjectCheckedOutByAnotherUserCannotBeCheckedOut( string objectName )
		{
			// Search for the sfd document.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( objectName );

			// Check the document out.
			listing.DoubleClickDocument( objectName ).WaitUntilLoaded().CheckOutClick();

			// Log out
			LoginPage login = home.TopPane.Logout();
			home = login.Login( this.mfContext.UsernameOfUser( "vaultadmin" ),
					this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );
			listing = home.SearchPane.QuickSearch( objectName );

			// Double-click the document.
			CheckOutDialog dialog = listing.DoubleClickDocument( objectName, false );
			Assert.That( !dialog.DoesDialogLoad() );

			// Verify the checked out status.
			Assert.AreEqual( CheckOutStatus.CheckedOutToOtherUser,
					listing.GetObjectCheckOutStatus( objectName ) );

			// Log out.
			home.TopPane.Logout();
			this.browserManager.EnsureQuitBrowser();
		}

		/// <summary>
		/// Double-clicks an MFD object to open it in a new view.
		/// </summary>
		/// <param name="objectName">Name of the MFD object.</param>
		[TestCase( "Invitation to Project Meeting 2/2007" )]
		public void DoubleClickOpenMFDObject( string objectName )
		{
			// Search for the mfd document.
			HomePage home = this.browserManager.StartTestAtHomePage();
			ListView listing = home.SearchPane.QuickSearch( objectName );

			// Open the mfd relationships by double clicking it.
			listing = listing.NavigateToView( objectName );
			home.MetadataCardPane.WaitUntilLoaded( objectName );
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );

			// Assert that relationships were expanded.
			Assert.AreEqual( 1, listing.NumberOfItems );
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( RelationshipsTreeStatus.Expanded, relationships.Status,
				"Mismatch between expected and actual relationships tree status." );
		}
	}
}
