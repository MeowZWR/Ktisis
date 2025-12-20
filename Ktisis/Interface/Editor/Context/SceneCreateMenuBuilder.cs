using System.IO;

using GLib.Popups.Context;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Factory.Types;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Editor.Context;

public class SceneCreateMenuBuilder {
	private readonly IEditorContext _ctx;

	private IEntityFactory Factory => this._ctx.Scene.Factory;

	public SceneCreateMenuBuilder(
		IEditorContext ctx
	) {
		this._ctx = ctx;
	}

	public ContextMenu Create() {
		return new ContextMenuBuilder()
			.Group(this.BuildActorGroup)
			.Separator()
			.Group(this.BuildLightGroup)
			.Separator()
			.Group(this.BuildUtilityGroup)
			.Build($"##SceneCreateMenu_{this.GetHashCode():X}");
	}

	private void BuildActorGroup(ContextMenuBuilder sub) {
		sub.Action("新建角色", () => this.Factory.CreateActor().Spawn())
			.Action("从文件导入角色", this.ImportCharaFromFile)
			.Action("添加场景角色", this._ctx.Interface.OpenOverworldActorList);
	}
	
	private void BuildLightGroup(ContextMenuBuilder sub)
		=> sub.SubMenu("新建光源", this.BuildLightMenu);
	
	private void BuildLightMenu(ContextMenuBuilder sub) {
		sub.Action("点光源", () => SpawnLight(LightType.PointLight))
			.Action("聚光灯", () => SpawnLight(LightType.SpotLight))
			.Action("面光源", () => SpawnLight(LightType.AreaLight))
			.Action("太阳光（定向）", () => SpawnLight(LightType.Directional))
			.Action("来自文件... (.ktlight)", () => this.ImportLightFromFile());
		
		void SpawnLight(LightType type) => this.Factory.CreateLight(type).Spawn();
	}

	private async void ImportLightFromFile() {
		this._ctx.Interface.OpenLightFile(async (path, file) => {
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			var newLight = await this.Factory.CreateLight().Spawn();
			await this._ctx.Scene.ApplyLightFile(newLight, file);
		});
	}

	private void BuildUtilityGroup(ContextMenuBuilder sub) {
		sub.Action("添加参考图像", this.OpenReferenceImage);
	}
	
	// Actor handling

	private void ImportCharaFromFile() {
		this._ctx.Interface.OpenCharaFile((path, file) => {
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			this.Factory.CreateActor()
				.WithAppearance(file)
				.SetName(name)
				.Spawn();
		});
	}
	
	// Reference image loading

	private void OpenReferenceImage() {
		this._ctx.Interface.OpenReferenceImages(path => {
			this.Factory.BuildRefImage()
				.SetPath(path)
				.Add()
				.Save();
		});
	}
}
