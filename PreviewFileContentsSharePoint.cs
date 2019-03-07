using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -9 )]
	[Parallelizable( ParallelScope.Self )]
	[Category( "SharePoint" )]
	[Category( "SharePointReadOnly" )]
	class PreviewFileContentsSharePoint : PreviewFileContents
	{
		protected override string classID => "PreviewFileContentsSharePoint";

		[OneTimeSetUp]
		public void SetApplicationMode()
		{
			this.configuration.ApplicationMode = ApplicationMode.Sharepoint;
		}
	}
}
