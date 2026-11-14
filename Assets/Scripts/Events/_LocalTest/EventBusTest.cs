// Local-only test helpers for EventBus (excluded from git).
public struct TestEvent : IEvent { public int Value; }

public static class EventBusTest
{
    public static string Log = "";
    public static void A(TestEvent e) { Log += "A" + e.Value + " "; }
    public static void B(TestEvent e) { Log += "B" + e.Value + " "; }
    public static void Self(TestEvent e) { Log += "S" + e.Value + " "; EventBus.Unsubscribe<TestEvent>(Self); }

    // Subscribed in one Play session to check the next session starts empty.
    public static void Leftover(TestEvent e) { Log += "LEFTOVER "; }
}
