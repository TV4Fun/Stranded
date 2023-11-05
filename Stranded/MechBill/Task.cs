using UnityEngine;

namespace Stranded.MechBill {
  public abstract class Task : ScriptableObject {
    public enum TaskStatus {
      Open,
      InProgress,
      Done,
      Cancelled
    }

    public delegate void TaskEvent(Task task);

    private MechBillJira _board;

    public MechBillJira Board {
      get => _board;
      set {
        _board = value;
        OnTaskCompleted = _board.OnTaskCompleted;
        OnTaskCancelled = _board.OnTaskCancelled;
      }
    }

    private MechBill _assignee;

    public TaskEvent OnTaskCompleted;
    public TaskEvent OnTaskCancelled;

    public MechBill Assignee {
      get => _assignee;
      set {
        _assignee = value;
        Status = TaskStatus.InProgress;
        _assignee.AssignedTask = this;
        OnAssigned();
      }
    }

    public TaskStatus Status { get; private set; } = TaskStatus.Open;

    public virtual void Cancel() {
      Status = TaskStatus.Cancelled;
      CancelImpl();
      OnTaskCancelled(this);
    }

    protected void Complete() {
      Status = TaskStatus.Done;
      OnTaskCompleted(this);
      _assignee.AssignedTask = null; // FIXME: Make this less hacky
      _assignee.GoHome();
      // _assignee.OnTaskCompleted();
    }

    public virtual void FixedUpdate() { }

    protected virtual void OnAssigned() { }

    protected abstract void CancelImpl();
  }
}
