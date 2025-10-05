using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Windows.Editors;

public class LightWindow : EntityEditWindow<LightEntity> {
	private readonly LocaleManager _locale;

	public LightWindow(
		IEditorContext ctx,
		LocaleManager locale
	) : base("光源编辑器", ctx) {
		this._locale = locale;
	}

	// Draw handlers
	
	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(400, 300),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	public override void Draw() {
		this.UpdateTarget();
		
		var s = this.Context.Selection;
		if (s.Count == 1) {
			var p = s.GetSelected().First();
			if (p is LightEntity l)
				this.SetTarget(l);
		}

		var entity = this.Target;
		
		ImGui.Text($"{entity.Name}：");
		ImGui.Spacing();

		using var _ = ImRaii.TabBar("##LightEditTabs");
		this.DrawTab("光源", this.DrawLightTab, entity);
		this.DrawTab("阴影", this.DrawShadowsTab, entity);
	}
	
	// Tabs

	private void DrawTab(string label, Action<LightEntity> draw, LightEntity entity) {
		using var _tab = ImRaii.TabItem(label);
		if (_tab.Success) draw.Invoke(entity);
	}
	
	// Light Tab

	private unsafe void DrawLightTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		ImGui.Spacing();
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
				ImGui.PushItemWidth(angleWidth);
				ImGui.SliderAngle("##AngleX", ref light->AreaAngle.X, -90, 90);
				ImGui.SameLine(0, angleSpace);
				ImGui.SliderAngle("光源角度##AngleY", ref light->AreaAngle.Y, -90, 90);
				ImGui.PopItemWidth();
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
	}
	
	// Shadows tab

	private unsafe void DrawShadowsTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		ImGui.Spacing();
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
	
	// Utility
	
	private unsafe void DrawLightFlag(string label, RenderLight* light, LightFlags flag) {
		var active = light->Flags.HasFlag(flag);
		if (ImGui.Checkbox(label, ref active))
			light->Flags ^= flag;
	}
}
