using ImGuiNET;

using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.Havok.Animation.Playback.Control.Default;

using Ktisis.Interop.Hooks;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	public static class AnimationControls {


		public static unsafe void Draw(IGameObject? target) {
			// Animation control
			if (ImGui.CollapsingHeader("动画控制")) {
				var control = PoseHooks.GetAnimationControl(target);
				if (PoseHooks.PosingEnabled || !Ktisis.IsInGPose || PoseHooks.IsGamePlaybackRunning(target) || control == null) {
					ImGui.Text("动画控制仅在以下条件可用:");
					ImGui.BulletText("全体/目标动作已暂停");
					ImGui.BulletText("姿势模式已禁用");
				} else
					AnimationSeekAndSpeed(control);
			}

		}
		public static unsafe void AnimationSeekAndSpeed(hkaDefaultAnimationControl* control) {
			var duration = control->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;
			var durationLimit = duration - 0.05f;

			if (control->hkaAnimationControl.LocalTime >= durationLimit)
				control->hkaAnimationControl.LocalTime = 0f;

			ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin() - GuiHelpers.GetRightOffset(ImGui.CalcTextSize("速度").X));
			ImGui.SliderFloat("进度", ref control->hkaAnimationControl.LocalTime, 0, durationLimit);
			ImGui.SliderFloat("速度", ref control->PlaybackSpeed, 0f, 0.999f);
			ImGui.PopItemWidth();
		}

	}
}
