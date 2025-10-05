using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class SkyEditor : EditorBase {
	private readonly SetTextureSelect _texSky;
	private readonly SetTextureSelect _texCloudTop;
	private readonly SetTextureSelect _texCloudSide;
	
	public override string Name { get; } = "天空";

	public SkyEditor(
		SetTextureSelect texSky,
		SetTextureSelect texCloudTop,
		SetTextureSelect texCloudSide
	) {
		this._texSky = texSky;
		this._texCloudTop = texCloudTop;
		this._texCloudSide = texCloudSide;
	}

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.SkyId) || flags.HasFlag(EnvOverride.Clouds);

	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("编辑天空盒", EnvOverride.SkyId, module);
        using (ImRaii.Disabled(!module.Override.HasFlag(EnvOverride.SkyId)))
			this._texSky.Draw("天空纹理", ref state.SkyId, id => $"bgcommon/nature/sky/texture/sky_{id:D3}.tex");
		
		ImGui.Spacing();
		ImGui.Spacing();

		this.DrawToggleCheckbox("编辑云", EnvOverride.Clouds, module);
		using var _clouds = ImRaii.Disabled(!module.Override.HasFlag(EnvOverride.Clouds));
		
		this._texCloudTop.Draw("上方云", ref state.Clouds.CloudTexture, id => $"bgcommon/nature/cloud/texture/cloud_{id:D3}.tex");
		this._texCloudSide.Draw("边缘云", ref state.Clouds.CloudSideTexture, id => $"bgcommon/nature/cloud/texture/cloudside_{id:D3}.tex");

		ImGui.ColorEdit3("云颜色", ref state.Clouds.CloudColor);
		ImGui.ColorEdit3("阴影颜色", ref state.Clouds.Color2);
		ImGui.SliderFloat("阴影", ref state.Clouds.Gradient, 0.0f, 2.0f);
		ImGui.SliderFloat("边缘高度", ref state.Clouds.SideHeight, 0.0f, 2.0f);
	}
}
