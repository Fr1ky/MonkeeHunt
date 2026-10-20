// Display data per enemy type. Recommended split is shown in the lobby only, never enforced.
public static class EnemyTypeInfo {

    public static string DisplayName(EnemyType type) {
        return type switch {
            EnemyType.Monkey => "Monkey",
            _ => type.ToString()
        };
    }

    public static void RecommendedSplit(EnemyType type, out int hunters, out int animals) {
        switch (type) {
            case EnemyType.Monkey:
            default:
                hunters = 5;
                animals = 3;
                break;
        }
    }
}
