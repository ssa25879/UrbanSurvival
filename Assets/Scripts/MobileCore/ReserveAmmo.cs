// 예비탄 가산 규칙(순수 로직): 상한을 넘기지 않는다. 상한이 음수이면 무제한 무기라 값을 바꾸지 않는다
public static class ReserveAmmo {
    public static int Add(int current, int amount, int cap) {
        if (cap < 0 || amount <= 0)
        {
            return current;
        }

        return System.Math.Min(current + amount, cap);
    }
}
