using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -2 )]
	[Parallelizable( ParallelScope.Self )]
	[Category( "SharePoint" )]
	[Category( "SharePointReadOnly" )]
	class QuickSearchSharePoint : QuickSearch
	{
		protected override string classID => "QuickSearchSharePoint";

		[OneTimeSetUp]
		public void SetApplicationMode()
		{
			this.configuration.ApplicationMode = ApplicationMode.Sharepoint;
		}

		// Override without Test attribute so that this test is not run in SharePoint.
		// Creating new objects is not yet implemented in vNext SharePoint.
		public override void SearchNewDocObjWhichYetToBeCheckedIn( 
			string objectName,
			string className,
			string template,
			string extension )
		{
			return;
		}
	}
}
