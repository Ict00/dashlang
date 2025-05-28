using dash.Execution.Units;

namespace dash.Execution.Standart;

public record OperatorCtx(DValue first, DValue second);
public record UnaryCtx(DValue val);