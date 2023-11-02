using UnityEngine;

namespace Stranded.MechBill {
  public abstract class Task : ScriptableObject {
    public enum TaskStatus {
      Open,
      InProgress,
      Done,
      Cancelled
    }

    public MechBillJira Board;

    public TaskStatus Status { get; private set; } = TaskStatus.Open;

    public virtual void Cancel() {
      Status = TaskStatus.Cancelled;
      CancelImpl();
      Board.OnTaskCancel(this);
    }

    protected void Complete() {
      Status = TaskStatus.Done;
    }

    protected abstract void CancelImpl();
  }
}
