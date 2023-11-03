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

    private MechBill _assignee;

    public MechBill Assignee {
      get => _assignee;
      set {
        _assignee = value;
        Status = TaskStatus.InProgress;
        _assignee.AssignedTask = this;
      }
    }

    public TaskStatus Status { get; private set; } = TaskStatus.Open;

    public virtual void Cancel() {
      Status = TaskStatus.Cancelled;
      CancelImpl();
      Board.OnTaskCancel(this);
    }

    protected void Complete() {
      Status = TaskStatus.Done;
      Board.OnTaskComplete(this);
    }

    public virtual void FixedUpdate() { }

    protected abstract void CancelImpl();
  }
}
