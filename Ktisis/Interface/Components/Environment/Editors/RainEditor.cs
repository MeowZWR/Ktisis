using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class RainEditor : EditorBase {
	public override string Name { get; } = "雨";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Rain);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("启用", EnvOverride.Rain, module);
		using var _ = this.Disable(module);

		ImGui.SliderFloat("强度", ref state.Rain.Intensity, 0.0f, 1.0f);
		ImGui.SliderFloat("厚度", ref state.Rain.Size, 0.0f, 1.0f);
		ImGui.ColorEdit4("颜色", ref state.Rain.Color);
		ImGui.Spacing();
		ImGui.SliderFloat("重量", ref state.Rain.Weight, 0.0f, 10.0f);
		ImGui.SliderFloat("散射", ref state.Rain.Scatter, 0.0f, 10.0f);
		ImGui.Spacing();
		ImGui.SliderFloat("雨滴", ref state.Rain.Raindrops, 0.0f, 1.0f);
	}
}
