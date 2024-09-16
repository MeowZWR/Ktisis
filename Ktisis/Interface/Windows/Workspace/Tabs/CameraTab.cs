using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Camera;
using Ktisis.Helpers;
using Ktisis.History;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class CameraTab {
		// History
		
		private static bool IsItemActive;
		private static CameraHistory? HistoryRecord;

		private static void RecordEdit(CameraEvent @event, string name, bool transTable = false, object? initVal = null) {
			var isEdited = transTable ? Transform.IsEdited : ImGui.IsItemDeactivatedAfterEdit();
			var isActive = transTable ? Transform.IsActive : ImGui.IsItemActivated();

			if (isEdited) {
				var history = HistoryRecord;
				HistoryRecord = null;
				
				IsItemActive = false;
				var camera = CameraService.GetActiveCamera();
				if (camera == null || history == null || history.Property != name) return;
				
				history.ResolveEndValue().AddToHistory();
			} else if (isActive && !IsItemActive) {
				var camera = CameraService.GetActiveCamera();
				if (camera == null) return;
				
				IsItemActive = true;
				HistoryRecord = HistoryItem.CreateCamera(@event)
					.SetSubject(camera)
					.SetProperty(name)
					.ResolveStartValue(initVal);
			}
		}

		private static void RecordEditImmediate(CameraEvent @event, string name, object? newVal) {
			var camera = CameraService.GetActiveCamera();
			if (camera == null) return;
			
			HistoryItem.CreateCamera(@event)
				.SetSubject(camera)
				.SetProperty(name)
				.ResolveStartValue()
				.SetEndValue(newVal)
				.AddToHistory();
		}

		// UI Code
		
		private static TransformTable Transform = new(); // this needs a rework.

		private static int? EditingId;
		
		public static void Draw() {
			ImGui.Spacing();
			
			DrawTargetLock();
			ImGui.Spacing();
			DrawCameraSelect();
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			DrawControls();

			ImGui.EndTabItem();
		}

		private static void DrawCameraSelect() {
			var camera = CameraService.GetActiveCamera();
			if (camera == null || !camera.IsValid()) return;
			
			var avail = ImGui.GetContentRegionAvail().X;
			var style = ImGui.GetStyle();
			
			var plusSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Plus);
			var camSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Camera);

			var cameras = CameraService.GetCameraList();

			var isFreecam = camera.WorkCamera != null;
			ImGui.BeginDisabled(isFreecam);
			
			var comboWidth = avail - style.ItemSpacing.X - (style.FramePadding.X * 4) - plusSize.X - camSize.X - 5;
			ImGui.SetNextItemWidth(comboWidth);
			if (ImGui.BeginCombo("##CameraSelect", camera.Name)) {
				var id = -1;
				foreach (var cam in cameras) {
					id++;
					
					if (EditingId == id) {
						var size = ImGui.GetContentRegionAvail();
						var padding = style.FramePadding.X;

						var canDelete = cam is { IsNative: false, WorkCamera: null };
						var buttonWidth = GuiHelpers.CalcIconSize(FontAwesomeIcon.Trash).X + padding * 2;
						var inputWidth = size.X - (canDelete ? buttonWidth + 5 : 0);
						
						ImGui.SetNextItemWidth(inputWidth);
						if (ImGui.InputTextWithHint("##CameraRename", "Camera name...", ref cam.Name, 32, ImGuiInputTextFlags.EnterReturnsTrue))
							EditingId = null;

						if (!canDelete) goto next;
						
						ImGui.SameLine(inputWidth + padding + 5);
						if (GuiHelpers.IconButtonHoldCtrlConfirm(FontAwesomeIcon.Trash, "删除（按住Ctrl）")) {
							CameraService.RemoveCamera(cam);
							EditingId = null;
							break;
						}

						next: continue;
					}

					var select = ImGui.Selectable($"{cam.Name}##CameraSelect_{id}");
					if (select) {
						if (cam.IsNative)
							CameraService.Reset();
						else
							CameraService.SetOverride(cam);
					} else if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
						EditingId = id;
					}
				}
				ImGui.EndCombo();
			} else if (EditingId != null) {
				EditingId = null;
			}

			ImGui.SameLine(ImGui.GetCursorPosX() + comboWidth + 5);
			var createNew = GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "新建摄像机");
			if (createNew) {
				Services.Framework.RunOnFrameworkThread(() => {
					var camera = CameraService.SpawnCamera();
					CameraService.SetOverride(camera);

					// Handle this here for cameras explicitly created by the user.
					// Disabling this for now, not sure how good it is for UX.
					/*HistoryItem.CreateCamera(CameraEvent.CreateCamera)
						.SetSubject(camera)
						.AddToHistory();*/
				});
			}
			
			ImGui.EndDisabled();

			ImGui.SameLine();
			var toggleFree = GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Camera, "开关工作摄像机");
			if (toggleFree)
				CameraService.ToggleFreecam();
		}

		private unsafe static void DrawTargetLock() {
			var camera = CameraService.GetActiveCamera();
			if (camera == null || !camera.IsValid()) return;

			var isFreecam = camera.WorkCamera != null;
			
			var tarLock = camera.GetOrbitTarget() is IGameObject actor ? (Actor*)actor.Address : null;
			var isTarLocked = tarLock != null || isFreecam;
			
			var target = tarLock != null ? tarLock : Ktisis.Target;
			if (target == null) return;
			
			ImGui.BeginDisabled(isFreecam);
			
			var icon = isTarLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var tooltip = isTarLocked ? "解锁轨道环绕对象" : "锁定轨道环绕对象";
			if (GuiHelpers.IconButtonTooltip(icon, tooltip, default, "CamOrbitLock")) {
				ushort? newVal = isTarLocked ? null : target->GameObject.ObjectIndex;
				RecordEditImmediate(CameraEvent.EditValue, "Orbit", newVal);
				camera.SetOrbit(newVal);
			}

			ImGui.SameLine();
			
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Sync, "移动摄像机到目标模型")) {
				var camTar = (Actor*)CameraService.GetTargetLock(camera.Address)?.Address;
				var goPos = camTar != null ? camTar->GameObject.Position : target->GameObject.Position;
				if (target->Model != null) {
					var doPos = target->Model->Position;
					var offset = doPos - (Vector3)goPos;
					RecordEditImmediate(CameraEvent.EditValue, "Offset", offset);
					camera.SetOffset(offset);
				}
			}

			ImGui.SameLine();
			ImGui.BeginDisabled(!isTarLocked);
			ImGui.Text(!isFreecam ? $"轨道环绕：{target->GetNameOrId()}" : "工作相机已启用。");
			ImGui.EndDisabled();
			
			ImGui.EndDisabled();
		}

		private unsafe static void DrawControls() {
			var camera = CameraService.GetActiveCamera();
			if (camera == null || !camera.IsValid()) return;

			var gposeCam = camera.AsGPoseCamera();

			// Camera position

			var camEdit = camera.CameraEdit;
			
			var pos = camera.Position;
			var offset = camEdit.Offset ?? Vector3.Zero;

			var posLock = camEdit.Position;
			var isFreecam = camera.WorkCamera != null;
			var isLocked = posLock != null || isFreecam;

			ImGui.BeginDisabled(isFreecam);
			var lockIcon = isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var lockTooltip = isLocked ? "解锁摄像机位置" : "锁定摄像机位置";
			if (GuiHelpers.IconButtonTooltip(lockIcon, lockTooltip, default, "CamPosLock")) {
				Vector3? newVal = isLocked ? null : pos - offset;
				RecordEditImmediate(CameraEvent.EditValue, "Position", newVal);
				camera.SetPositionLock(newVal);
			}
			ImGui.EndDisabled();

			ImGui.SameLine();

			var posCursor = ImGui.GetCursorPosX();
			ImGui.BeginDisabled(!isLocked);
			if (Transform.ColoredDragFloat3("##CamWorldPos", ref pos, 0.005f)) {
				if (camera.WorkCamera is WorkCamera freecam) {
					freecam.Position = pos;
					freecam.InterpPos = pos;
				} else {
					camera.SetPositionLock(pos - offset);
				}
			}
			RecordEdit(isFreecam ? CameraEvent.FreecamValue : CameraEvent.EditValue, "Position", true);
			ImGui.EndDisabled();
			
			ImGui.BeginDisabled(isFreecam); // Below controls disabled under freecam.
			
			// Camera offset
			
			ImGui.Dummy(default);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(FontAwesomeIcon.Plus, "基础位置偏移");
			ImGui.SameLine();
			ImGui.SetCursorPosX(posCursor);
			if (Transform.ColoredDragFloat3("##CamOffset", ref offset, 0.005f))
				camera.SetOffset(offset != Vector3.Zero ? offset : null);
			RecordEdit(CameraEvent.EditValue, "Offset", true);

			// Angle

			ImGui.Spacing();
			ImGui.Spacing();
			
			PrepareIconTooltip(FontAwesomeIcon.ArrowsSpin, "摄像机环绕轨道角度", posCursor);
			var angle = gposeCam->Angle * MathHelpers.Rad2Deg;
			if (Transform.DragFloat2("CameraAngle", ref angle, Ktisis.Configuration.TransformTableBaseSpeedRot))
				gposeCam->Angle = angle * MathHelpers.Deg2Rad;
			RecordEdit(CameraEvent.CameraValue, "Angle", true);

			// Pan

			ImGui.Spacing();
			
			PrepareIconTooltip(FontAwesomeIcon.ArrowsAlt, "摄像机摇摄角度", posCursor);
			var pan = gposeCam->Pan * MathHelpers.Rad2Deg;
			if (Transform.DragFloat2("CameraPan", ref pan, Ktisis.Configuration.TransformTableBaseSpeedRot))
				gposeCam->Pan = pan * MathHelpers.Deg2Rad;
			RecordEdit(CameraEvent.CameraValue, "Pan", true);

			ImGui.EndDisabled(); // Above controls disabled under freecam.
			
			// other shit

			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginDisabled(isFreecam);
			PrepareIconTooltip(FontAwesomeIcon.CameraRotate, "摄像机旋转角度", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			var initRot = gposeCam->Rotation * 1f;
			ImGui.SliderAngle("##CamRotato", ref gposeCam->Rotation, -180, 180, "%.3f", ImGuiSliderFlags.AlwaysClamp);
			RecordEdit(CameraEvent.CameraValue, "Rotation", false, initRot);
			ImGui.EndDisabled();

			PrepareIconTooltip(FontAwesomeIcon.VectorSquare, "视场角", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			var initFov = gposeCam->FoV * 1f;
			ImGui.SliderAngle("##CamFoV", ref gposeCam->FoV, -40, 100, "%.3f", ImGuiSliderFlags.AlwaysClamp);
			RecordEdit(CameraEvent.CameraValue, "FoV", false, initFov);

			ImGui.BeginDisabled(isLocked);
			PrepareIconTooltip(FontAwesomeIcon.Moon, "摄像机距离", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			var initDist = gposeCam->Distance * 1f;
			ImGui.SliderFloat("##CamDist", ref gposeCam->Distance, 0, gposeCam->DistanceMax);
			RecordEdit(CameraEvent.CameraValue, "Distance", false, initDist);
			ImGui.EndDisabled();
			
			// Delimit & Collision

			ImGui.Spacing();
			ImGui.BeginDisabled(isFreecam);

			var delimit = gposeCam->DistanceMax > 20;
			if (ImGui.Checkbox("摄像机距离解限", ref delimit)) {
				var max = delimit ? 350 : 20;
				gposeCam->DistanceMax = max;
				gposeCam->DistanceMin = delimit ? 0 : 1.5f;
				gposeCam->Distance = Math.Clamp(gposeCam->Distance, 0, max);
				gposeCam->YMin = delimit ? 1.5f : 1.25f;
				gposeCam->YMax = delimit ? -1.5f : -1.4f;
			}

			ImGui.SameLine();
			
			ImGui.Checkbox("禁用摄像机碰撞", ref camera.CameraEdit.NoClip);
			
			ImGui.EndDisabled();
		}

		private static void PrepareIconTooltip(FontAwesomeIcon icon, string tooltip, float posCursor) {
			ImGui.Dummy(default);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(icon, tooltip);
			ImGui.SameLine();
			ImGui.SetCursorPosX(posCursor);
		}
	}
}
