import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Collections;

public class JarLaunchProbe {
    public static void main(String[] args) throws Exception {
        Files.write(Paths.get("jar-launch-result.txt"), Collections.singletonList("JAR launched"));
    }
}
