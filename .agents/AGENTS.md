# Unity Physics & Collision Rules

## Projectiles and Self-Collision
- **Rule**: Bất kỳ vật thể nào được ném/bắn ra (ví dụ: đạn, lựu đạn, bom) PHẢI được gán đúng Layer (ví dụ: `Ragdoll` hoặc một layer `EnemyProjectile` riêng biệt) để không xảy ra hiện tượng va chạm vật lý (Self-collision) với chính nhân vật/quái vật ném ra nó.
- **Context**: Nếu để Layer mặc định, collider của quả bom sẽ đè lên collider của quái vật, khiến Unity's Physics Engine đẩy quả bom bay đi lệch quỹ đạo hoặc rơi xuống đất. Luôn ghi nhớ kiểm tra Layer của prefab đạn/bom trước khi thêm vào logic ném.
