package application.util;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;
import java.util.Properties;

public class FileUtils {

	public static Path writeProperties(String propertieFileName, Map<String, String> properties) throws IOException {

		Path path = Paths.get(System.getProperty("user.home"), "cp_configs", propertieFileName);
		File file = path.toFile();

		if (file.isFile()) {
			saveProperties(properties, file);
		} else {
			createInitialFile(path);
			saveProperties(properties, file);
		}
		return path;

	}

	private static void createInitialFile(Path path) throws IOException {
		Files.createDirectories(path.getParent());
		Files.createFile(path);
	}

	private static void saveProperties(Map<String, String> properties, File file) throws IOException {
		InputStream iStream = new FileInputStream(file);
		Properties prop = new Properties();

		prop.putAll(properties);
		prop.store(new FileOutputStream(file), null);
		iStream.close();
	}

	public static Boolean fileExist(String absolutePath) throws FileNotFoundException {
		if (absolutePath == null || absolutePath.trim().equals("")) {
			throw new FileNotFoundException("Camino (absolutePath) vacío");
		}
		File file = new File(absolutePath);

		if (file != null && file.isFile()) {
			return true;
		} else {
			return false;
		}
	}

	public static String getLogoPath() throws IOException {
		Path path = Paths.get(System.getProperty("user.home"), "cp_configs", "ReportImage.properties");
		Properties props = new Properties();
		File file = path.toFile();
		
		if (!file.isFile()) {
			Map<String,String> initialProps = new HashMap<>();
			initialProps.put("carimboPath","");
			initialProps.put("logoPath","");
			file = writeProperties("ReportImage.properties", initialProps).toFile();
		}
		
		InputStream inStream = new FileInputStream(file);	
		props.load(inStream);
		
		String resposta = props.getProperty("logoPath").trim().equals("") ?  "images/logo.png" : props.getProperty("logoPath");
		
		return resposta;
	}

	public static String getCarimboPath() throws IOException {
		Path path = Paths.get(System.getProperty("user.home"), "cp_configs", "ReportImage.properties");
		Properties props = new Properties();
		File file = path.toFile();
		
		if (!file.isFile()) {
			Map<String,String> initialProps = new HashMap<>();
			initialProps.put("carimboPath","");
			initialProps.put("logoPath","");
			file = writeProperties("ReportImage.properties", initialProps).toFile();
		}
		
		InputStream inStream = new FileInputStream(file);	
		props.load(inStream);
		
		String resposta =  props.getProperty("carimboPath").trim().equals("") ?  "images/carimbo.png" : props.getProperty("carimboPath");
		
		return resposta;
	}

	public static void writeConnectionProperties(Map<String, String> configs) throws IOException {
		Path path = Paths.get(System.getProperty("user.home"), "cp_configs", "db.properties");
		File file = path.toFile();
		if (!file.isFile()) {
			createInitialFile(path);
		}
		BufferedWriter bw = new BufferedWriter(new FileWriter(file));
		bw.write("#" + (new Date()).toString());
		bw.newLine();
		for (String key : configs.keySet()) {
			bw.write(key + "=" + configs.get(key));
			bw.newLine();
		}
		bw.write("useSSL=" + "false");
		bw.close();
	}

	public static Properties loadConnetionProperties() throws IOException {
		Path path = Paths.get(System.getProperty("user.home"), "cp_configs", "db.properties");
		File file = path.toFile();
		if (!file.isFile()) {
			createInitialFile(path);
		}
		BufferedReader br = new BufferedReader(new FileReader(file));
		Properties props = new Properties();
		props.load(br);
		return props;
	}
}
