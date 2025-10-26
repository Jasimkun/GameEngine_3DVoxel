/* * IDamageable.cs
 * 이 스크립트는 모든 적(Enemy, Teleport 등)이 
 * 공통으로 사용할 "신분증" 역할을 합니다.
 */
public interface IDamageable
{
    // 이 "신분증"을 가진 스크립트는
    // 반드시 TakeDamage(int damage) 함수를 가지고 있어야 합니다.
    void TakeDamage(int damage);
}