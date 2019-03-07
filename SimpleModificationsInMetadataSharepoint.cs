using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -10 )]
	[Parallelizable( ParallelScope.Self )]
	[Category( "SharePoint" )]
	[Category( "SharePointEdit" )]
	class SimpleModificationsInMetadataSharePoint : SimpleModificationsInMetadata
	{
		protected override string classID => "SimpleModificationsInMetadataSharePoint";

		[OneTimeSetUp]
		public void SetApplicationMode()
		{
			this.configuration.ApplicationMode = ApplicationMode.Sharepoint;
		}

		/// <summary>
		/// Testing that metadatacard header is expanded and collapsed and settings retained
		/// when navigating between different objects. In SharePoint, the metadata card header
		/// is collapsed by default.
		/// </summary>		
		[Test]
		[Category( "MetadataCard" )]
		[TestCase(
			"2. Manage Customers",
			"CBH International",
			"OMCC Corporation" )]
		public override void CollapseAndExpandMetadataCardHeader( string viewToNavigate, string object1Name, string object2Name )
		{
			// Additional assertion message variable declaration.
			string additionalAssertMessage = "Mismatch between the expected and actual metadatacard header state.";

			// Start the test at home page.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.ListView.NavigateToView( viewToNavigate );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( object1Name );

			// Assert that metadatacard in collapsed state.
			Assert.AreEqual( MetadataCardHeaderStatus.Collapsed, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Expand the metadatacard header.
			mdCard.HeaderOptionRibbon.ExpandHeader();

			// Assert that metadatacard in expanded state.
			Assert.AreEqual( MetadataCardHeaderStatus.Expanded, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Select another object in the view.
			mdCard = listing.SelectObject( object2Name );

			// Assert that metadatacard in expanded state.
			Assert.AreEqual( MetadataCardHeaderStatus.Expanded, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Collapse the metadatacard header.
			mdCard.HeaderOptionRibbon.CollapseHeader();

			// Assert that metadatacard in collapsed state.
			Assert.AreEqual( MetadataCardHeaderStatus.Collapsed, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Select another object in the view.
			mdCard = listing.SelectObject( object1Name );

			// Assert that metadatacard in collapsed state.
			Assert.AreEqual( MetadataCardHeaderStatus.Collapsed, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );
		}
	}


}
