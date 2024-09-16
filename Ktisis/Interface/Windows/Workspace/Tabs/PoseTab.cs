using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Helpers;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class PoseTab {
		public static TransformTable Transform = new();
		
		public static PoseContainer _TempPose = new();
		
		public unsafe static void Draw(IGameObject target) {
			var cfg = Ktisis.Configuration;

			var actor = (Actor*)target.Address;

			// Extra Controls
			ControlButtons.DrawExtra();

			// Parenting

			var parent = cfg.EnableParenting;
			if (ImGui.Checkbox("父级", ref parent))
				cfg.EnableParenting = parent;

			if (actor->Model != null) {
				// Transform table
				TransformTable(actor);

				ImGui.Spacing();

				// Bone categories
				if (ImGui.CollapsingHeader("骨骼类型")) {
					if (!Categories.DrawToggleList(cfg)) {
						ImGui.Text("未找到骨骼");
						ImGui.Text("点击 (");
						ImGui.SameLine();
						GuiHelpers.Icon(FontAwesomeIcon.EyeSlash);
						ImGui.SameLine();
						ImGui.Text(") 来显示骨骼");
					}
				}

				// Bone tree
				BoneTree.Draw(actor);
			} else {
				ImGui.Text("目标没有有效的骨架！");
				ImGui.Spacing();
			}

			// Import & Export
			if (ImGui.CollapsingHeader("导入 & 导出"))
				ImportExportPose(actor);

			// Advanced
			if (ImGui.CollapsingHeader("高级（调试）")) {
				DrawAdvancedDebugOptions(actor);
			}

			ImGui.EndTabItem();
		}
		
		public static unsafe void DrawAdvancedDebugOptions(Actor* actor) {
			if (actor->Model != null) {
				if (ImGui.Button("重设当前姿势"))
					actor->Model->SyncModelSpace();
				if (ImGui.Button("设置为参考姿势"))
					actor->Model->SyncModelSpace(true);
				if (ImGui.Button("存储姿势"))
					_TempPose.Store(actor->Model->Skeleton);
				ImGui.SameLine();
				if (ImGui.Button("应用姿势"))
					_TempPose.Apply(actor->Model->Skeleton);
			}

			if(ImGui.Button("强制重绘"))
				actor->Redraw();
		}
		
		// Transform Table actor and bone names display, actor related extra

		private static unsafe bool TransformTable(Actor* target) {
			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone();

			if (!select.Active) return Transform.Draw(target);
			if (bone == null) return false;

			return Transform.Draw(bone);
		}
		
		public unsafe static void ImportExportPose(Actor* actor) {
			ImGui.Spacing();
			ImGui.Text("变换");

			// Transforms

			var trans = Ktisis.Configuration.PoseTransforms;

			var rot = trans.HasFlag(PoseTransforms.Rotation);
			if (ImGui.Checkbox("旋转##ImportExportPose", ref rot))
				trans = trans.ToggleFlag(PoseTransforms.Rotation);

			var pos = trans.HasFlag(PoseTransforms.Position);
			var col = pos;
			ImGui.SameLine();
			if (col) ImGui.PushStyleColor(ImGuiCol.Text, 0xff00fbff);
			if (ImGui.Checkbox("位置##ImportExportPose", ref pos))
				trans = trans.ToggleFlag(PoseTransforms.Position);
			if (col) ImGui.PopStyleColor();

			var scale = trans.HasFlag(PoseTransforms.Scale);
			col = scale;
			ImGui.SameLine();
			if (col) ImGui.PushStyleColor(ImGuiCol.Text, 0xff00fbff);
			if (ImGui.Checkbox("缩放##ImportExportPose", ref scale))
				trans = trans.ToggleFlag(PoseTransforms.Scale);
			if (col) ImGui.PopStyleColor();

			if (trans > PoseTransforms.Rotation) {
				ImGui.TextColored(
					Workspace.ColYellow,
                    "* 导入可能产生意外结果。"
                );
			}

			Ktisis.Configuration.PoseTransforms = trans;

			ImGui.Spacing();
			ImGui.Text("模式");

			// Modes

			var modes = Ktisis.Configuration.PoseMode;

			var body = modes.HasFlag(PoseMode.Body);
			if (ImGui.Checkbox("身体##ImportExportPose", ref body))
				modes = modes.ToggleFlag(PoseMode.Body);

			var face = modes.HasFlag(PoseMode.Face);
			ImGui.SameLine();
			if (ImGui.Checkbox("表情##ImportExportPose", ref face))
				modes = modes.ToggleFlag(PoseMode.Face);
			
			var wep = modes.HasFlag(PoseMode.Weapons);
			ImGui.SameLine();
			if (ImGui.Checkbox("武器##ImportExportPose", ref wep))
				modes = modes.ToggleFlag(PoseMode.Weapons);

			var posWep = Ktisis.Configuration.PositionWeapons;
			if (modes.HasFlag(PoseMode.Weapons)) {
				ImGui.Spacing();
				if (ImGui.Checkbox("将位置应用于武器##ApplyWepPos", ref posWep))
					Ktisis.Configuration.PositionWeapons = posWep;
			}

			Ktisis.Configuration.PoseMode = modes;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			var isUseless = trans == 0 || modes == 0;

			if (isUseless) ImGui.BeginDisabled();
			if (ImGui.Button("导入##ImportExportPose")) {
				KtisisGui.FileDialogManager.OpenFileDialog(
					"导入姿势",
					"姿势文件{.pose,.cmp}",
					(success, path) => {
						if (!success) return;

						PoseHelpers.ImportPose(actor, path, Ktisis.Configuration.PoseMode);
					},
					1,
					null
				);
			}
			if (isUseless) ImGui.EndDisabled();
			ImGui.SameLine();
			if (ImGui.Button("导出##ImportExportPose")) {
				KtisisGui.FileDialogManager.SaveFileDialog(
					"导出姿势",
					"姿势文件 (.pose){.pose}",
					"Untitled.pose",
					".pose",
					(success, path) => {
						if (!success) return;

						PoseHelpers.ExportPose(actor, path, Ktisis.Configuration.PoseMode);
					}
				);
			}

			ImGui.Spacing();
		}
	}
}
