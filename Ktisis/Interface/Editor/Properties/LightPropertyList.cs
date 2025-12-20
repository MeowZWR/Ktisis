using System;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;
using Ktisis.Editor.Context.Types;

namespace Ktisis.Interface.Editor.Properties;

public class LightPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly LocaleManager _locale;

	public LightPropertyList(
		IEditorContext ctx,
		LocaleManager locale
	) {
		this._ctx = ctx;
		this._locale = locale;
	}
	
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not LightEntity light)
			return;
		
		builder.AddHeader("光源", () => this.DrawLightTab(light));
		builder.AddHeader("阴影", () => this.DrawShadowsTab(light));
	}

	private unsafe void DrawLightTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		this.DrawLightFlag("启用反射", light, LightFlags.Reflection);
		ImGui.Spacing();
		
		// Light type
		
		var lightTypePreview = this._locale.Translate($"lightType.{light->LightType}");
		if (ImGui.BeginCombo("光源类型", lightTypePreview)) {
			foreach (var value in Enum.GetValues<LightType>()) {
				var valueLabel = this._locale.Translate($"lightType.{value}");
				if (ImGui.Selectable(valueLabel, light->LightType == value))
					light->LightType = value;
			}
			ImGui.EndCombo();
		}
		
		switch (light->LightType) {
			case LightType.SpotLight:
				ImGui.SliderFloat("锥角##LightAngle", ref light->LightAngle, 0.0f, 180.0f, "%0.0f 度");
				ImGui.SliderFloat("衰减角##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f 度");
				break;
			case LightType.AreaLight:
				var angleSpace = ImGui.GetStyle().ItemInnerSpacing.X;
				var angleWidth = ImGui.CalcItemWidth() / 2 - angleSpace;
				using (var _ = ImRaii.ItemWidth(angleWidth)) {
					ImGui.SliderAngle("##AngleX", ref light->AreaAngle.X, -90, 90);
					ImGui.SameLine(0, angleSpace);
					ImGui.SliderAngle("光源角度##AngleY", ref light->AreaAngle.Y, -90, 90);
				}
				ImGui.SliderFloat("衰减角##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f 度");
				break;
			
		}
		
		ImGui.Spacing();
		
		// Falloff
		
		var falloffPreview = this._locale.Translate($"lightFalloff.{light->FalloffType}");
		if (ImGui.BeginCombo("衰减类型", falloffPreview)) {
			foreach (var value in Enum.GetValues<FalloffType>()) {
				var valueLabel = this._locale.Translate($"lightFalloff.{value}");
				if (ImGui.Selectable(valueLabel, light->FalloffType == value))
					light->FalloffType = value;
			}
			ImGui.EndCombo();
		}

		ImGui.DragFloat("衰减强度##FalloffPower", ref light->Falloff, 0.01f, 0.0f, 1000.0f);
		
		// Base light settings
		
		ImGui.Spacing();
		
		var color = light->Color.RGB;
		if (ImGui.ColorEdit3("颜色", ref color, ImGuiColorEditFlags.Hdr | ImGuiColorEditFlags.Uint8))
			light->Color.RGB = color;
		ImGui.DragFloat("强度", ref light->Color.Intensity, 0.01f, 0.0f, 100.0f);
		if (ImGui.DragFloat("范围##LightRange", ref light->Range, 0.1f, 0, 999))
			entity.Flags |= LightEntityFlags.Update;

		ImGui.Spacing();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, "Import light settings"))
			this._ctx.Interface.OpenLightFile((path, file) => this._ctx.Scene.ApplyLightFile(entity, file));

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Save, "Export light settings"))
			this._ctx.Interface.OpenLightExport(entity);
	}

	private unsafe void DrawShadowsTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		this.DrawLightFlag("动态阴影", light, LightFlags.Dynamic);
		ImGui.Spacing();
		
		this.DrawLightFlag("投射角色阴影", light, LightFlags.CharaShadow);
		this.DrawLightFlag("投射物体阴影", light, LightFlags.ObjectShadow);

		ImGui.Spacing();
		ImGui.DragFloat("阴影范围", ref light->CharaShadowRange, 0.1f, 0.0f, 1000.0f);
		ImGui.Spacing();
		ImGui.DragFloat("阴影近距", ref light->ShadowNear, 0.01f, 0.0f, 1000.0f);
		ImGui.DragFloat("阴影远距", ref light->ShadowFar, 0.01f, 0.0f, 1000.0f);
	}
	private unsafe void DrawLightFlag(string label, RenderLight* light, LightFlags flag) {
		var active = light->Flags.HasFlag(flag);
		if (ImGui.Checkbox(label, ref active))
			light->Flags ^= flag;
	}
}
