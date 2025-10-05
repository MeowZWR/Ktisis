using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class FogEditor : EditorBase {
	public override string Name { get; } = "雾";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Fog);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("启用", EnvOverride.Fog, module);
		using var _ = this.Disable(module);

		ImGui.ColorEdit4("颜色", ref state.Fog.Color);
		ImGui.SliderFloat("距离", ref state.Fog.Distance, 0.0f, 1000.0f);
		ImGui.SliderFloat("厚度", ref state.Fog.Thickness, 0.0f, 100.0f);
		ImGui.Spacing();
		ImGui.SliderFloat("不透明度", ref state.Fog.Opacity, 0.0f, 1.0f);
		ImGui.SliderFloat("天空能见度", ref state.Fog.SkyVisibility, 0.0f, 1.0f);
	}
}
