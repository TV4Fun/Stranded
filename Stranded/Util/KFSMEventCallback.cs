namespace Stranded.Util {
  // ReSharper disable once InconsistentNaming
  public readonly struct KFSMEventCallback {
    private readonly KerbalFSM _fsm;
    private readonly KFSMEvent _event;

    public KFSMEventCallback(KerbalFSM fsm, KFSMEvent @event) {
      _fsm = fsm;
      _event = @event;
    }

    public override int GetHashCode() {
      return _fsm.GetHashCode() ^ _event.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is KFSMEventCallback cb) {
        return this == cb;
      }
      return false;
    }

    public static bool operator ==(KFSMEventCallback lhs, KFSMEventCallback rhs) {
      return lhs._fsm.Equals(rhs._fsm) && lhs._event.Equals(rhs._event);
    }

    public static bool operator !=(KFSMEventCallback lhs, KFSMEventCallback rhs) {
      return !(lhs == rhs);
    }

    private void Invoke() {
      _fsm.RunEvent(_event);
    }

    public static implicit operator Callback(KFSMEventCallback callback) {
      return callback.Invoke;
    }

    public static implicit operator KFSMCallback(KFSMEventCallback callback) {
      return callback.Invoke;
    }
  }
}
