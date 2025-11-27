using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour, IInputAxisOwner
{
    public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
    {
        axes.Add(new IInputAxisOwner.AxisDescriptor {
            DrivenAxis = () => ref screenOffsetX,
            Name = "Screen Offset X",
            Hint = IInputAxisOwner.AxisDescriptor.Hints.X
        });
        axes.Add(new IInputAxisOwner.AxisDescriptor() {
            DrivenAxis = () => ref screenOffsetY,
            Name = "Screen Offset Y",
            Hint = IInputAxisOwner.AxisDescriptor.Hints.Y
        });
        axes.Add(new IInputAxisOwner.AxisDescriptor() {
            DrivenAxis = () => ref lensFov,
            Name = "Lens Fov",
            Hint = IInputAxisOwner.AxisDescriptor.Hints.Y
        });
        axes.Add(new IInputAxisOwner.AxisDescriptor() {
            DrivenAxis = () => ref orbitalSwitch,
            Name = "Orbital Switch",
            Hint = IInputAxisOwner.AxisDescriptor.Hints.X
        });
    }

    [SerializeField] InputAxis screenOffsetX;
    [SerializeField] InputAxis screenOffsetY;
    [SerializeField] InputAxis lensFov;
    [SerializeField] CinemachineCamera cinemachineCamera;
    [SerializeField] CinemachineCameraOffset cameraOffset;
    [SerializeField] CinemachineInputAxisController inputAxisController;

    float lastOrbitalSwitchValue;
    InputAxis orbitalSwitch;

    void Awake()
    {
        orbitalSwitch = InputAxis.DefaultMomentary;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        cinemachineCamera.Lens.FieldOfView = lensFov.Value;
        cameraOffset.Offset = new Vector3(screenOffsetX.Value, screenOffsetY.Value);

        if (Mathf.Approximately(orbitalSwitch.Value, 1))
        {
            if (!Mathf.Approximately(lastOrbitalSwitchValue, orbitalSwitch.Value))
            {
                inputAxisController.enabled = !inputAxisController.enabled;
                Cursor.lockState = inputAxisController.enabled ? CursorLockMode.Locked : CursorLockMode.None;
            }
        }
        lastOrbitalSwitchValue = orbitalSwitch.Value;
    }
}
