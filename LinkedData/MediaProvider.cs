using Sitecore.Resources.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkedData
{
    public class MediaProvider : Sitecore.Resources.Media.MediaProvider
    {
        MediaCreator _creator;

        public override Sitecore.Resources.Media.MediaCreator Creator
        {
            get
            {
                if (_creator == null)
                {
                    _creator = new MediaCreator();
                }
                return (Sitecore.Resources.Media.MediaCreator)_creator;
            }
            set
            {
            }
        }
    }
}
