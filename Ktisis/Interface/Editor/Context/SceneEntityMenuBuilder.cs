using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Dalamud.Bindings.ImGui;
using GLib.Popups.Context;

using Ktisis.Data.Files;
using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Nodes;
using Ktisis.Interface.Widgets;
using Ktisis.Scene;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Editor.Context;

public class SceneEntityMenuBuilder {
	private readonly IEditorContext _ctx;
	private readonly SceneEntity _entity;

	private IEditorInterface Ui => this._ctx.Interface;

	public SceneEntityMenuBuilder(
		IEditorContext ctx,
		SceneEntity entity
	) {
		this._ctx = ctx;
		this._entity = entity;
	}

	public ContextMenu Create() {
		return new ContextMenuBuilder()
			.Group(this.BuildEntityBaseTop)
			.Group(this.BuildEntityType)
			.Group(this.BuildEntityBaseBottom)
			.Build($"EntityContextMenu_{this.GetHashCode():X}");
	}

	private void BuildEntityBaseTop(ContextMenuBuilder menu) {
		if (!this._entity.IsSelected)
			menu.Action("选择", () => this._entity.Select(SelectMode.Multiple));
		else
			menu.Action("取消选择", this._entity.Unselect);
		if (this._entity.Children.Any())
			menu.Action("选择层级", () => {
				foreach (var entity in this._entity.Children.Where(entity => !entity.IsSelected))
					entity.Select(SelectMode.Multiple);
				if (!this._entity.IsSelected) this._entity.Select(SelectMode.Multiple);
			});

		if (this._entity.Root is ActorEntity actorEntity)
			menu.SubMenu("预设...", sub => {
				foreach (var (name, isEnabled) in actorEntity.GetPresets()) {
					sub.CheckableAction(name, isEnabled != PresetState.Disabled, () => actorEntity.TogglePreset(name));
				}

				sub.Separator()
					.Action("保存新预设", () => this.Ui.OpenSavePreset(actorEntity));
			});
	}

	private void BuildEntityBaseBottom(ContextMenuBuilder menu) {
		if (this._entity is IAttachable attach && attach.IsAttached())
			menu.Separator().Action("分离", () => this._ctx.Posing.Attachments.Detach(attach));

		menu.Separator().Action("重命名", () => this.Ui.OpenRenameEntity(this._entity));

		if (this._entity is IDeletable deletable) {
			menu.Separator();
			if (this._entity is ActorEntity actor)
				menu.Action("复制", () => this.DuplicateActor(actor));
			if (this._entity is LightEntity light)
				menu.Action("复制", () => this.DuplicateLight(light));
			menu.Action("删除", () => deletable.Delete());
		}
	}
	
	// Entity types

	private void BuildEntityType(ContextMenuBuilder menu) {
		switch (this._entity) {
			case ActorEntity actor:
				this.BuildActorMenu(menu, actor);
				break;
			case EntityPose pose:
				this.BuildPoseMenu(menu, pose);
				break;
			case LightEntity light:
				this.BuildLightMenu(menu, light);
				break;
		}
	}

	private void OpenEditor() => this.Ui.OpenEditorFor(this._entity);
	
	// Actors

	private unsafe void BuildActorMenu(ContextMenuBuilder menu, ActorEntity actor) {
		menu.Separator()
			.Action("设为目标", actor.Actor.SetGPoseTarget)
			.Separator()
			.Action("编辑外观", this.OpenEditor)
			.Group(sub => this.BuildActorIpcMenu(sub, actor))
			.Separator()
			.SubMenu("导入...", sub => {
				var builder = sub.Action("角色文件 (.chara)", () => this.Ui.OpenCharaImport(actor))
					.Action("NPC", () => this.Ui.OpenCharaImport(actor, true))
					.Action("姿势文件 (.pose)", () => this.Ui.OpenPoseImport(actor));

				if (this._ctx.Plugin.Ipc.IsAnyMcdfActive && actor.GetHuman() != null) {
					builder.Action("Mare数据 (.mcdf)", () => {
						this.Ui.OpenMcdfFile(path => this.ImportMcdf(actor, path));
					});
				}
			})
			.SubMenu("导出...", sub => {
				sub.Action("角色文件 (.chara)", () => this.Ui.OpenCharaExport(actor))
					.Action("姿势文件 (.pose)", () => this.ExportPose(actor.Pose));
			});
	}

	private unsafe void BuildActorIpcMenu(ContextMenuBuilder menu, ActorEntity actor) {
		menu.SubMenu("IPC 外观", sub => {
			if (this._ctx.Plugin.Ipc.IsPenumbraActive) {
				sub.Action("Penumbra: 分配合集", () => this.Ui.OpenAssignCollection(actor));
				sub.Action("Penumbra: 隐形皮肤", () => this._ctx.Characters.Mcdf.SetInvisibleSkin(actor));
			}
			if (this._ctx.Plugin.Ipc.IsGlamourerActive)
				sub.Action("Glamourer: 应用设计", () => this.Ui.OpenApplyDesign(actor));
			if (this._ctx.Plugin.Ipc.IsCustomizeActive)
				sub.Action("Customize: 分配配置文件", () => this.Ui.OpenAssignCProfile(actor));
			if (this._ctx.Plugin.Ipc.IsAnyMcdfActive && actor.GetHuman() != null)
				sub.Action("还原IPC数据", () => this._ctx.Characters.Mcdf.Revert(actor.Actor));
		});
	}

	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
	}

	private async void DuplicateActor(ActorEntity actor) {
		// pack actor into a temp charafile to apply to new actor after creation
		var file = await this._ctx.Characters.SaveCharaFile(actor);
		var dupe = await this._ctx.Scene.Factory.CreateActor()
			.WithAppearance(file)
			.Spawn();

		// copy glamourer state if applicable
		if (!this._ctx.Plugin.Ipc.IsGlamourerActive) return;
		var ipc = this._ctx.Plugin.Ipc.GetGlamourerIpc();
		ipc.CopyState(actor.Actor.ObjectIndex, dupe.Actor.ObjectIndex);
	}
	
	// Poses

	private void BuildPoseMenu(ContextMenuBuilder menu, EntityPose pose) {
		menu.Separator()
			.Action("导入姿势", () => this.ImportPose(pose))
			.Action("导出姿势", () => this.ExportPose(pose))
			.Separator()
			.Action("设为参考姿势", () => this._ctx.Posing.ApplyReferencePose(pose));
	}

	private void ImportPose(EntityPose pose) {
		if (pose.Parent is ActorEntity actor)
			this.Ui.OpenPoseImport(actor);
	}
	
	private async void ExportPose(EntityPose? pose) {
		if (pose == null) return;
		await this.Ui.OpenPoseExport(pose);
	}
	
	// Lights

	private void BuildLightMenu(ContextMenuBuilder menu, LightEntity light) {
		menu.Separator()
			.Action("编辑灯光", this.OpenEditor)
			.Separator()
			.Action("导入灯光文件", () => this.Ui.OpenLightFile((path, file) => this.ImportLight(light, file)))
			.Action("导出灯光文件", () => this.Ui.OpenLightExport(light));
	}

	private async void ImportLight(LightEntity light, LightFile file) {
		await this._ctx.Scene.ApplyLightFile(light, file);
	}

	private async void DuplicateLight(LightEntity light) {
		var file = await this._ctx.Scene.SaveLightFile(light);
		var newLight = await this._ctx.Scene.Factory.CreateLight().Spawn();
		this.ImportLight(newLight, file);
	}
}
