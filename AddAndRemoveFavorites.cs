using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -4 )]
	[Parallelizable( ParallelScope.Self )]
	class AddAndRemoveFavorites
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

		public AddAndRemoveFavorites()
		{
			this.classID = "AddAndRemoveFavorites";
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

		/// <summary>
		/// After all tests have been run, ensure that the browser is closed. Then destroy the 
		/// test environment of the test class.
		/// </summary>
		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}


		/// <summary>
		/// After each test, navigate back to home page if test has passed. But if the test has failed,
		/// then the browser is closed to ensure fresh start for next test.
		/// </summary>
		[TearDown]
		public void EndTest()
		{

			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Add object to favorites by using context menu. Then verify the star icon and that object is visible in favorites view.
		/// Then locate object again and remove from favorites by using context menu. Then verify the star icon and that object is not
		/// visible in favorites view.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Invoice LCC556-4.pdf", "Document", Description = "Document object." )]
		[TestCase( "A&A Consulting (AEC)", "Customer", Description = "Non-document object." )]
		public void AddAndRemoveFavoritesByContextMenu( string objectName, string objectType )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.RightClickItemOpenContextMenu( objectName ).AddToFavorites();

			// Verify that the star icon is lit in the metadata card.
			Assert.AreEqual( FavoritesStatus.Favorite, mdCard.HeaderOptionRibbon.FavoritesStatus );

			// Go to favorites view.
			listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Favorites );

			Assert.True( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is not found in favorites view after it was added to favorites." );

			// Locate the object again in search.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			mdCard = listing.RightClickItemOpenContextMenu( objectName ).RemoveFromFavorites();

			// Verify that the star icon is un-lit in the metadata card.
			Assert.AreEqual( FavoritesStatus.NotFavorite, mdCard.HeaderOptionRibbon.FavoritesStatus );

			// Navigate to home and then go to the favorites view by clicking it in listing.
			homePage.TopPane.TabButtons.HomeTabClick();
			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			listing = homePage.ListView.NavigateToView( "Favorites" );


			Assert.False( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is still found in favorites view after it was removed from favorites." );
		}

		/// <summary>
		/// Add object to favorites. Then go to favorites view and remove the object from favorites by using
		/// context menu. The object should instantly disappear from the favorites view.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "South Elevation.dwg", "Document" )]
		public void RemoveFromFavoritesInFavoritesViewByContextMenu( string objectName, string objectType )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Add object to favorites.
			listing.RightClickItemOpenContextMenu( objectName ).AddToFavorites();

			// Go to favorites view.
			listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Favorites );

			// Remove object from favorites, while in favorites view. This causes the object to immediately
			// disappear from view and the metadata card selection is cleared.
			listing.RightClickItemOpenContextMenu( objectName ).RemoveFromFavoritesClearsSelection();

			Assert.False( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is still found in favorites view after it was removed from favorites." );
		}

		/// <summary>
		/// Add object to favorites by using metadata card option ribbon. Then verify the star icon and that object is visible 
		/// in favorites view. Then locate object again and remove from favorites by using metadata card option ribbon. 
		/// Then verify the star icon and that object is not visible in favorites view.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Marketing Meeting Agenda.doc", "Document", Description = "Document object" )]
		[TestCase( "Sandra Williams", "Contact person", Description = "Non-document object." )]
		public void AddAndRemoveFavoritesByMetadataCardOptionRibbon( string objectName, string objectType )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add object to favorites.
			mdCard.HeaderOptionRibbon.AddToFavorites();

			// Verify that the star icon is lit in the metadata card.
			Assert.AreEqual( FavoritesStatus.Favorite, mdCard.HeaderOptionRibbon.FavoritesStatus );

			// Navigate to home and then go to the favorites view by clicking it in listing.
			homePage.TopPane.TabButtons.HomeTabClick();
			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			listing = homePage.ListView.NavigateToView( "Favorites" );

			Assert.True( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is not found in favorites view after it was added to favorites." );

			// Locate the object again in search.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			mdCard = listing.SelectObject( objectName );

			mdCard.HeaderOptionRibbon.RemoveFromFavorites();

			// Verify that the star icon is un-lit in the metadata card.
			Assert.AreEqual( FavoritesStatus.NotFavorite, mdCard.HeaderOptionRibbon.FavoritesStatus );

			// Go to favorites view.
			listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Favorites );

			Assert.False( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is still found in favorites view after it was removed from favorites." );
		}

		/// <summary>
		/// Add object to favorites. Then go to favorites view and remove the object from favorites by using
		/// metadata card option ribbon. The object should instantly disappear from the favorites view.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Web Site Graphics for ESTT", "Project" )]
		public void RemoveFromFavoritesInFavoritesViewByMetadataCardOptionRibbon( string objectName, string objectType )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Add object to favorites.
			MetadataCardPopout popoutMDCard = listing.SelectObject( objectName ).PopoutMetadataCard();
			popoutMDCard.HeaderOptionRibbon.AddToFavorites();
			popoutMDCard.CloseButtonClick();

			// Go to favorites view.
			listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Favorites );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Remove object from favorites, while in favorites view. This causes the object to immediately
			// disappear from view and the metadata card selection is cleared.
			mdCard.HeaderOptionRibbon.RemoveFromFavoritesClearsSelection();

			Assert.False( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is still found in favorites view after it was removed from favorites." );
		}
	}
}
