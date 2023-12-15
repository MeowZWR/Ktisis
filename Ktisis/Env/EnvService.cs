﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

using Ktisis.Events;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Env;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Env {
	public static class EnvService {
		public static float? TimeOverride;
		public static uint? SkyOverride;
		public static bool? FreezeWater;
		
		// Init & Dispose
		
		public static void Init() {
			EventManager.OnGPoseChange += OnGPoseChange;
			EnvHooks.Init();

			unsafe {
				var env = EnvManager.Instance();
				Logger.Information($"-> {(nint)env:X} {(nint)env->EnvScene:X} {(nint)env->EnvSpace:X}");

				//var con = *(nint*)(Services.SigScanner.Module.BaseAddress + 0x21ABEA0);
				//PluginLog.Information($"EnvRender: {con:X}");
				
				var t = env->EnvScene;
				for (var i = 0; i < 1; i++) {
					var s = (EnvSpace*)t->EnvSpaces + i;
					var addr = (nint)s;
					Logger.Information($"{(nint)s:X} {(nint)s->EnvLocation:X} {s->DrawObject.Object.Position}");
					
				}

				var world = LayoutWorld.Instance();
				Logger.Information($"World: {(nint)world:X} {(nint)world->ActiveLayout:X}");
			}
		}

		public static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;
			EnvHooks.Dispose();
		}
		
		// Events
		
		private static void OnGPoseChange(bool state) {
			EnvHooks.SetEnabled(state);
			if (!state) {
				TimeOverride = null;
				SkyOverride = null;
				FreezeWater = null;
			}
		}
		
		// Data
		
		private static uint CurSky = uint.MaxValue;

		public static readonly object SkyLock = new();
		public static IDalamudTextureWrap? SkyTex;
		
		public static void GetSkyImage(uint sky) {
			if (sky == CurSky) return;
		
			CurSky = sky;
			GetSkyboxTex(CurSky).ContinueWith(result => {
				if (result.Exception != null) {
					PluginLog.Error(result.Exception.ToString());
					return;
				}

				lock (SkyLock) {
					SkyTex?.Dispose();
					SkyTex = result.Result;
				}
			});
		}
		
		private static async Task<IDalamudTextureWrap?> GetSkyboxTex(uint skyId) {
			await Task.Yield();
			PluginLog.Verbose($"Retrieving skybox texture: {skyId:000}");
			return Services.Textures.GetTextureFromGame($"bgcommon/nature/sky/texture/sky_{skyId:000}.tex");
		}
		
		public unsafe static byte[] GetEnvWeatherIds() {
			var env = (EnvManagerEx*)EnvManager.Instance();
			var scene = env != null ? env->EnvScene : null;
			if (scene == null) return Array.Empty<byte>();
			return scene->GetWeatherSpan()
				.TrimEnd((byte)0)
				.ToArray();
		}

		public static async Task<IEnumerable<WeatherInfo>> GetWeatherIcons(IEnumerable<byte> weathers, CancellationToken token) {
			await Task.Yield();
			
			var result = new List<WeatherInfo>();

			var weatherSheet = Services.DataManager.GetExcelSheet<Weather>();
			if (weatherSheet == null) return result;

			foreach (var id in weathers) {
				if (token.IsCancellationRequested) break;
				
				var weather = weatherSheet.GetRow(id);
				if (weather == null) continue;

				var icon = Services.Textures.GetIcon((uint)weather.Icon);
				var info = new WeatherInfo(weather, icon);
				result.Add(info);
			}
			
			token.ThrowIfCancellationRequested();
			
			return result;
		}
	}
}
