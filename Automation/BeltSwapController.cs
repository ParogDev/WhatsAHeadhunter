using System;
using System.Collections;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using WhatsAHeadhunter.Detection;

namespace WhatsAHeadhunter.Automation;

public enum BeltSwapState
{
    Idle,
    OpeningInventory,
    WaitingForInventory,
    Unequipping,
    WaitingForCursor,
    Reequipping,
    ClosingInventory,
    Cooldown,
}

public enum BeltSwapMode
{
    Disabled = 0,
    KeyPress = 1,
    Automatic = 2,
}

public class BeltSwapController
{
    private BeltSwapState _state = BeltSwapState.Idle;
    private DateTime _lastSwapAttempt;
    private DateTime _stateEnteredAt;
    private bool _inventoryWasOpen;
    private Coroutine? _activeCoroutine;

    public BeltSwapState State => _state;
    public bool IsSwapping => _state != BeltSwapState.Idle && _state != BeltSwapState.Cooldown;
    public string StatusText { get; private set; } = "";

    public void Update(
        GameController gc,
        WhatsAHeadhunterSettings settings,
        ShroudWalkerDetector detector)
    {
        var mode = (BeltSwapMode)settings.BeltSwap.Mode.Value;
        if (mode == BeltSwapMode.Disabled) return;

        // Don't trigger unless Shroud Walker is confirmed active
        if (!detector.IsActive) return;

        bool shouldTrigger = false;

        if (mode == BeltSwapMode.KeyPress)
        {
            var key = settings.BeltSwap.Hotkey.Value;
            if (key != Keys.None && Input.GetKeyState(key))
                shouldTrigger = true;
        }
        else if (mode == BeltSwapMode.Automatic)
        {
            shouldTrigger = true;
        }

        if (!shouldTrigger) return;
        if (_state != BeltSwapState.Idle) return;

        // Cooldown check
        var cooldownMs = settings.BeltSwap.SwapCooldownMs.Value;
        if ((DateTime.UtcNow - _lastSwapAttempt).TotalMilliseconds < cooldownMs) return;

        // Safety checks
        if (!CanSafelyInteract(gc)) return;

        // Start the swap
        StartSwap(gc, settings);
    }

    private bool CanSafelyInteract(GameController gc)
    {
        // Game must be focused
        if (!gc.Window.IsForeground()) return false;

        // Player must not be clicking
        if (Input.IsKeyDown(Keys.LButton)) return false;

        // No fullscreen panels
        var ingameUi = gc.IngameState.IngameUi;
        foreach (var panel in ingameUi.FullscreenPanels)
        {
            if (panel.IsVisible) return false;
        }

        // No large panels (except inventory which we might need)
        foreach (var panel in ingameUi.LargePanels)
        {
            if (panel.IsVisible && panel != ingameUi.InventoryPanel)
                return false;
        }

        // Chat must not be open
        if (ingameUi.ChatPanel?.ChatTitlePanel?.IsVisible == true) return false;

        return true;
    }

    private void StartSwap(GameController gc, WhatsAHeadhunterSettings settings)
    {
        _lastSwapAttempt = DateTime.UtcNow;
        _inventoryWasOpen = gc.IngameState.IngameUi.InventoryPanel.IsVisible;

        var coroutine = new Coroutine(SwapCoroutine(gc, settings), null, "HeadhunterBeltSwap");
        Core.ParallelRunner.Run(coroutine);
        _activeCoroutine = coroutine;
    }

    private IEnumerator SwapCoroutine(GameController gc, WhatsAHeadhunterSettings settings)
    {
        var clickDelay = settings.BeltSwap.ClickDelayMs.Value;
        var ingameUi = gc.IngameState.IngameUi;

        // Step 1: Open inventory if needed
        if (!ingameUi.InventoryPanel.IsVisible)
        {
            SetState(BeltSwapState.OpeningInventory, "Opening inventory...");
            yield return Input.KeyPress(Keys.I);
            yield return new WaitTime(clickDelay * 3);

            // Wait for inventory to appear (up to 500ms)
            SetState(BeltSwapState.WaitingForInventory, "Waiting for inventory...");
            var waitStart = DateTime.UtcNow;
            while (!ingameUi.InventoryPanel.IsVisible)
            {
                if ((DateTime.UtcNow - waitStart).TotalMilliseconds > 500)
                {
                    SetState(BeltSwapState.Idle, "Failed: inventory did not open");
                    yield break;
                }
                yield return new WaitTime(20);
            }
        }

        // Step 2: Get belt slot
        var beltSlot = ingameUi.InventoryPanel[InventoryIndex.Belt];
        if (beltSlot == null || !beltSlot.IsVisible)
        {
            SetState(BeltSwapState.Idle, "Failed: belt slot not found");
            yield break;
        }

        // Step 3: Unequip belt (left click on belt slot)
        SetState(BeltSwapState.Unequipping, "Unequipping belt...");

        // Final safety: check mouse not held right before clicking
        if (Input.IsKeyDown(Keys.LButton))
        {
            SetState(BeltSwapState.Idle, "Aborted: mouse button held");
            yield break;
        }

        var beltCenter = beltSlot.GetClientRectCache.Center;
        Input.SetCursorPos(new System.Numerics.Vector2(beltCenter.X, beltCenter.Y));
        yield return new WaitTime(clickDelay);
        Input.Click(MouseButtons.Left);
        yield return new WaitTime(clickDelay * 2);

        // Step 4: Wait briefly for cursor to hold item
        SetState(BeltSwapState.WaitingForCursor, "Belt on cursor...");
        yield return new WaitTime(clickDelay);

        // Step 5: Re-equip (click same slot again)
        SetState(BeltSwapState.Reequipping, "Re-equipping belt...");

        // Re-read belt slot position in case it shifted
        beltSlot = ingameUi.InventoryPanel[InventoryIndex.Belt];
        if (beltSlot != null && beltSlot.IsVisible)
        {
            beltCenter = beltSlot.GetClientRectCache.Center;
            Input.SetCursorPos(new System.Numerics.Vector2(beltCenter.X, beltCenter.Y));
            yield return new WaitTime(clickDelay);
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(clickDelay);
        }

        // Step 6: Close inventory if we opened it
        if (!_inventoryWasOpen)
        {
            SetState(BeltSwapState.ClosingInventory, "Closing inventory...");
            yield return new WaitTime(clickDelay);
            yield return Input.KeyPress(Keys.I);
            yield return new WaitTime(clickDelay);
        }

        // Done
        SetState(BeltSwapState.Cooldown, "Belt swapped - buffs cleared");
        yield return new WaitTime(settings.BeltSwap.SwapCooldownMs.Value);
        SetState(BeltSwapState.Idle, "");
    }

    private void SetState(BeltSwapState state, string status)
    {
        _state = state;
        _stateEnteredAt = DateTime.UtcNow;
        StatusText = status;
    }

    public void Reset()
    {
        _state = BeltSwapState.Idle;
        StatusText = "";
        _activeCoroutine = null;
    }
}
