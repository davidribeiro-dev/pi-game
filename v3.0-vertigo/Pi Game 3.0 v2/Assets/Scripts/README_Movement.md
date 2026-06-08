# Movement Scripts (PlayerMovement + MouseLook)

Quick setup instructions for the included first-person movement scripts.

1) Project setup — New Input System (required)
- Install the **Input System** package (Package Manager) and open Player Settings → Other Settings → Active Input Handling → set to **Input System Package (New)**. Restart the editor if prompted.

2) Create a Player GameObject
- Create an empty GameObject and name it `Player`.
- Add a `CharacterController` component to `Player`.
- Add a `PlayerInput` component to `Player` and assign your `Input Actions` asset (for example `InputSystem_Actions`). Set the Default Map to your gameplay map (commonly `Gameplay`).
- Attach the `PlayerMovement` script to `Player`.

3) Add a Camera
- Create a Camera as a child of `Player` (e.g., `Player/Camera`).
- Attach the `MouseLook` script to the Camera.
- In the Camera's `MouseLook` inspector, set `Player Body` to the `Player` transform.
- In the `PlayerMovement` inspector, set `Camera Transform` to the child Camera (optional but recommended for camera-relative movement).

4) Input Actions required (names must match)
- The migration makes the scripts rely on the new Input System and read actions from the `PlayerInput` component. Ensure your Input Actions asset contains these actions (typically within a `Gameplay` action map):
  - `Move` (Vector2) — WASD / left stick
  - `Look` (Vector2) — Mouse delta / right stick (set to "Pass Through")
  - `Jump` (Button)
  - `Sprint` (Button or Value)
  - `Crouch` (Button or Value)

5) Default runtime controls (assuming default bindings)
- Move: `W/A/S/D` or left stick
- Sprint: `Left Shift` (hold)
- Crouch: `Left Control` or `C`
- Jump: `Space`
- Toggle cursor lock: `Escape` (MouseLook uses the keyboard Escape key via the new Input System)

6) Tuning
- Configure `walkSpeed`, `sprintSpeed`, `jumpHeight`, `gravity`, and `mouseSensitivity` in the inspector.
- `standingHeight` and `crouchHeight` control `CharacterController.height` when standing and crouched.

7) Notes
- The scripts now require a `PlayerInput` component on the `Player` GameObject. They read actions by name from `playerInput.actions`.
- If you prefer generated C# wrappers (strongly typed), enable `Generate C# Class` on your Input Actions asset and I can convert the scripts to use the generated class.

If you want I can also:
- Wire a prefab example with `PlayerInput` and the action asset assigned.
- Convert the scripts to use a generated C# class instead of string action lookups.
