using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.NET
{
    public class KeenClient
    {
        private ProjectSettings _prjSettings;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prjSettings">A ProjectSettings instance containing the ProjectId and API keys</param>
		public KeenClient(ProjectSettings prjSettings)
        {
			// Preconditions
            if (null==prjSettings)
                throw new KeenException("An ProjectSettings instance is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.ProjectId))
                throw new KeenException("A Project ID is required.");
            if ((string.IsNullOrWhiteSpace(prjSettings.MasterKey)
                && string.IsNullOrWhiteSpace(prjSettings.WriteKey)))
                throw new KeenException("A Master or Write API key is required.");

            _prjSettings = prjSettings;
        }
    }
}
