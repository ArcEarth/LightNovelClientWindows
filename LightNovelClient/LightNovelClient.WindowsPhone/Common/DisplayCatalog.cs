using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace LightNovel.Common
{
	enum DisplayType
	{
		WVGA_120,  // 480x800	@4"
		WXGA_200,  // 768x1280	@4.5"
		P720_180,  // 720x1280	@4.7"
		QHD_120,   // 540x960	@5"
		P1080_240, // 1080x1920	@5.5"
		P720_140,  // 720x1280	@6"
		P1080_220, // 1080x1920	@6"
	}
	//class DisplayCatalogHelper
	//{
	//	public static DisplayType DisplayCatalog
	//	{
	//		get
	//		{
	//			var dipInfo = DisplayInformation.GetForCurrentView();
	//			var scl = dipInfo.RawPixelsPerViewPixel;
	//			//var raw = dipInfo.			
	//		}
	//	}
	//}
}
