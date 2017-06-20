﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Cms;
using Nop.Core.Domain.Cms;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Security;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Controllers
{
    public partial class WidgetController : BaseAdminController
	{
		#region Fields

        private readonly IWidgetService _widgetService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly WidgetSettings _widgetSettings;
	    private readonly IPluginFinder _pluginFinder;

        #endregion

        #region Ctor

        public WidgetController(IWidgetService widgetService,
            IPermissionService permissionService,
            ISettingService settingService,
            WidgetSettings widgetSettings,
            IPluginFinder pluginFinder)
		{
            this._widgetService = widgetService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._widgetSettings = widgetSettings;
            this._pluginFinder = pluginFinder;
        }

		#endregion 
        
        #region Methods
        
        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual IActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            return View();
        }

        [HttpPost]
        public virtual IActionResult List(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedKendoGridJson();

            var widgetsModel = new List<WidgetModel>();
            var widgets = _widgetService.LoadAllWidgets();
            foreach (var widget in widgets)
            {
                var tmp1 = widget.ToModel();
                tmp1.IsActive = widget.IsWidgetActive(_widgetSettings);
                widgetsModel.Add(tmp1);
            }
            widgetsModel = widgetsModel.ToList();
            var gridModel = new DataSourceResult
            {
                Data = widgetsModel,
                Total = widgetsModel.Count()
            };

            return Json(gridModel);
        }

        [HttpPost]
        public virtual IActionResult WidgetUpdate(WidgetModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            var widget = _widgetService.LoadWidgetBySystemName(model.SystemName);
            if (widget.IsWidgetActive(_widgetSettings))
            {
                if (!model.IsActive)
                {
                    //mark as disabled
                    _widgetSettings.ActiveWidgetSystemNames.Remove(widget.PluginDescriptor.SystemName);
                    _settingService.SaveSetting(_widgetSettings);
                }
            }
            else
            {
                if (model.IsActive)
                {
                    //mark as active
                    _widgetSettings.ActiveWidgetSystemNames.Add(widget.PluginDescriptor.SystemName);
                    _settingService.SaveSetting(_widgetSettings);
                }
            }

            var pluginDescriptor = widget.PluginDescriptor;

            //display order
            pluginDescriptor.DisplayOrder = model.DisplayOrder;
            PluginFileParser.SavePluginDescriptionFile(pluginDescriptor);

            //reset plugin cache
            _pluginFinder.ReloadPlugins();

            return new NullJsonResult();
        }

        public virtual IActionResult ConfigureWidget(string systemName)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            var widget = _widgetService.LoadWidgetBySystemName(systemName);
            if (widget == null)
                //No widget found with the specified id
                return RedirectToAction("List");

            var url = widget.GetConfigurationPageUrl();
            //TODO implement logic when configuration page is not required
            return Redirect(url);
        }

        public virtual IActionResult WidgetsByZone(string widgetZone)
        {
            //model
            var model = new List<RenderWidgetModel>();

            var widgets = _widgetService.LoadActiveWidgetsByWidgetZone(widgetZone);
            foreach (var widget in widgets)
            {
                widget.GetDisplayWidgetRoute(out string viewComponentName, out RouteValueDictionary viewComponentArguments);

                var widgetModel = new RenderWidgetModel
                {
                    WidgetViewComponentName = viewComponentName,
                    WidgetViewComponentArguments = viewComponentArguments,
                };

                model.Add(widgetModel);
            }

            return PartialView(model);
        }

	    #endregion
    }
}