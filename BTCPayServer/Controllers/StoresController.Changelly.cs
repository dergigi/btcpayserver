﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments.Changelly;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Controllers
{
    public partial class StoresController
    {
        [HttpGet]
        [Route("{storeId}/changelly")]
        public IActionResult UpdateChangellySettings(string storeId)
        {
            var store = HttpContext.GetStoreData();
            if (store == null)
                return NotFound();
            UpdateChangellySettingsViewModel vm = new UpdateChangellySettingsViewModel();
            SetExistingValues(store, vm);
            return View(vm);
        }

        private void SetExistingValues(StoreData store, UpdateChangellySettingsViewModel vm)
        {

            var existing = store.GetStoreBlob().ChangellySettings;
            if (existing == null)
                return;
            vm.ApiKey = existing.ApiKey;
            vm.ApiSecret = existing.ApiSecret;
            vm.ApiUrl = existing.ApiUrl;
            vm.ChangellyMerchantId = existing.ChangellyMerchantId;
            vm.Enabled = existing.Enabled;
            vm.AmountMarkupPercentage = existing.AmountMarkupPercentage;
        }

        [HttpPost]
        [Route("{storeId}/changelly")]
        public async Task<IActionResult> UpdateChangellySettings(string storeId, UpdateChangellySettingsViewModel vm,
            string command)
        {
            var store = HttpContext.GetStoreData();
            if (store == null)
                return NotFound();
            if (vm.Enabled)
            {
                if (!ModelState.IsValid)
                {
                    return View(vm);
                }
            }

            var changellySettings = new ChangellySettings()
            {
                ApiKey = vm.ApiKey,
                ApiSecret = vm.ApiSecret,
                ApiUrl = vm.ApiUrl,
                ChangellyMerchantId = vm.ChangellyMerchantId,
                Enabled = vm.Enabled,
                AmountMarkupPercentage = vm.AmountMarkupPercentage
            };

            switch (command)
            {
                case "save":
                    var storeBlob = store.GetStoreBlob();
                    storeBlob.ChangellySettings = changellySettings;
                    store.SetStoreBlob(storeBlob);
                    await _Repo.UpdateStore(store);
                    TempData[WellKnownTempData.SuccessMessage] = "Changelly settings modified";
                    _changellyClientProvider.InvalidateClient(storeId);
                    return RedirectToAction(nameof(UpdateStore), new
                    {
                        storeId
                    });
                case "test":
                    try
                    {
                        var client = new Changelly(_httpClientFactory.CreateClient(), changellySettings.ApiKey, changellySettings.ApiSecret,
                            changellySettings.ApiUrl);
                        var result = await client.GetCurrenciesFull();
                        TempData[WellKnownTempData.SuccessMessage] = "Test Successful";
                        return View(vm);
                    }
                    catch (Exception ex)
                    {
                        TempData[WellKnownTempData.ErrorMessage] = ex.Message;
                        return View(vm);
                    }
                default:
                    return View(vm);
            }
        }
    }
}
