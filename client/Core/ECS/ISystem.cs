namespace Guildmaster.Client.Core.ECS;

public interface ISystem
{
    void Update(float deltaTime);
    void Draw();
}
