using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Interface.Components.Files;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Windows.Import;

public class PoseImportDialog : EntityEditWindow<ActorEntity> {
	private readonly FileSelect<PoseFile> _select;

	public PoseImportDialog(
		IEditorContext ctx,
		FileSelect<PoseFile> select
	) : base(
		"导入姿势",
		ctx,
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._select = select;
		select.OnOpenDialog = this.OnFileDialogOpen;
	}
	
	private void OnFileDialogOpen(FileSelect<PoseFile> sender) {
		this.Context.Interface.OpenPoseFile(sender.SetFile);
	}

	// Draw UI

	public override void Draw() {
		this.UpdateTarget();
		
		ImGui.Text($"正在为 {this.Target.Name} 导入姿势");
		ImGui.Spacing();

		this.DrawEmbed();
	}

	public void DrawEmbed() {
		this.PreDraw();
		if (!this.Context.IsValid) return; // despite closing in predraw, we might continue to later draw funcs, so stop drawing here
		using var _id = ImRaii.PushId($"PoseEmbed_{this.GetHashCode():X}");

		this._select.Draw();
		
		ImGui.Spacing();
		this.DrawPoseApplication();
		ImGui.Spacing();
	}
	
	// Pose application

	private void DrawPoseApplication() {
		using var _ = ImRaii.Disabled(!this._select.IsFileOpened);
		
		var isSelectBones = this.Target.Recurse()
			.Where(child => child is SkeletonNode)
			.Any(child => child.IsSelected);
		
		this.DrawTransformSelect();
		ImGui.Spacing();
		this.DrawApplyModes(isSelectBones);
		ImGui.Spacing();
		ImGui.Spacing();

		if (ImGui.Button("应用"))
			this.ApplyPoseFile(isSelectBones);
	}

	private void DrawTransformSelect() {
		ImGui.Text("变换：");

		var file = this.Context.Config.File;
		var trans = file.ImportPoseTransforms;

		var rotation = trans.HasFlag(PoseTransforms.Rotation);
		if (ImGui.Checkbox("旋转##PoseImportRot", ref rotation))
			file.ImportPoseTransforms ^= PoseTransforms.Rotation;
		
		ImGui.SameLine();

		// if operating on a .cmp file, force disable position and scale as they're always dummy
		var isCmp = this._select.Selected != null && this._select.Selected.Path.EndsWith(".cmp");

		var position = trans.HasFlag(PoseTransforms.Position);
		var scale = trans.HasFlag(PoseTransforms.Scale);
		using var _ = ImRaii.Disabled(isCmp);
		if (isCmp) {
			position = false;
			file.ImportPoseTransforms &= ~PoseTransforms.Position;
			scale = false;
			file.ImportPoseTransforms &= ~PoseTransforms.Scale;
		}

		if (ImGui.Checkbox("位置##PoseImportPos", ref position))
			file.ImportPoseTransforms ^= PoseTransforms.Position;
		
		ImGui.SameLine();

		if (ImGui.Checkbox("缩放##PoseImportScale", ref scale))
			file.ImportPoseTransforms ^= PoseTransforms.Scale;
	}

	private void DrawApplyModes(bool isSelectBones) {
		ImGui.Text("模式：");

		var file = this.Context.Config.File;
		var modes = file.ImportPoseModes;

		var isSelectiveImport = file.ImportPoseSelectedBones && isSelectBones;
		using (ImRaii.Disabled(!isSelectBones)) {
			if (ImGui.Checkbox("仅应用选中骨骼", ref isSelectiveImport))
				file.ImportPoseSelectedBones ^= true;
		}

		if (isSelectiveImport) {
			using (ImRaii.PushIndent()) {
				ImGui.Checkbox("包含子级", ref file.SelectedBonesIncludeDescendants);

				var hasPosition = file.ImportPoseTransforms.HasFlag(PoseTransforms.Position);
				using (ImRaii.Disabled(!hasPosition))
					ImGui.Checkbox("锚定分组位置", ref file.AnchorPoseSelectedBones);
			}
		}

		if (!isSelectiveImport || file.SelectedBonesIncludeDescendants) {
			var body = modes.HasFlag(PoseMode.Body);
			if (ImGui.Checkbox("身体##PoseImportBody", ref body))
				file.ImportPoseModes ^= PoseMode.Body;

			ImGui.SameLine();

			var face = modes.HasFlag(PoseMode.Face);
			if (ImGui.Checkbox("面部##PoseImportFace", ref face))
				file.ImportPoseModes ^= PoseMode.Face;
			if (face && this._select.IsFileOpened && this.Target.Pose?.HasDTFace() != _select.Selected?.File.HasDTFace()) {
				ImGui.SameLine();
				Icons.DrawIcon(FontAwesomeIcon.ExclamationTriangle, ColorHelpers.RgbaVector4ToUint(ImGuiColors.DalamudYellow));
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("面部将不会从所选姿势文件导入，因为与所选角色不兼容。");
			}
		}

		ImGui.Checkbox("排除耳朵骨骼", ref file.ExcludePoseEarBones);

		if (this._select.IsFileOpened && this.Context.Posing.IsIkEnabled) {
			ImGui.Spacing();
			Icons.DrawIcon(FontAwesomeIcon.ExclamationTriangle, ColorHelpers.RgbaVector4ToUint(ImGuiColors.DalamudYellow));
			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
			ImGui.TextWrapped("逆向动力学已启用！它们可能会覆盖导入的肢体。");
		}
	}
	
	// Apply pose

	private void ApplyPoseFile(bool isSelectBones) {
		var file = this._select.Selected?.File;
		if (file == null) return;

		var pose = this.Target.Pose;
		if (pose == null) return;

		var cfg = this.Context.Config.File;
		var selectedBones = isSelectBones && cfg.ImportPoseSelectedBones;
		var includeDescendants = cfg.SelectedBonesIncludeDescendants;
		var anchorGroups = cfg.AnchorPoseSelectedBones;
		var excludeEars = cfg.ExcludePoseEarBones;
		this.Context.Posing.ApplyPoseFile(pose, file, cfg.ImportPoseModes, cfg.ImportPoseTransforms, selectedBones, includeDescendants, anchorGroups, excludeEars);
	}
}
