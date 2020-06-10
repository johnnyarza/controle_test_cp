package application.util;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;

public class  FileUtils {
	
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
	
	public static String getLogoPath() throws  IOException {
		File file = new File("src/main/resources/config/ReportImage.properties");
		String logoPath = "";
		String resposta = "images/logo.png";
		try (BufferedReader br = new BufferedReader(new FileReader(file))) {
			logoPath = br.readLine();
			while (logoPath != null) {
				if (logoPath.substring(0,2).equals("1:")) {
					if (logoPath != null && !logoPath.trim().equals("") && FileUtils.fileExist(logoPath.substring(2))) {
						resposta = logoPath.substring(2);
					}				
				}
				logoPath = br.readLine();
			}
			return resposta;
		}
	}
	
	public static String getCarimboPath() throws IOException {
		File file = new File("src/main/resources/config/ReportImage.properties");
		String carimboPath = "";
		String resposta = "images/carimbo.png";
		try (BufferedReader br = new BufferedReader(new FileReader(file))) {
			carimboPath = br.readLine();
			while (carimboPath != null) {
				if (carimboPath.substring(0, 2).equals("2:")) {
					if (carimboPath != null && !carimboPath.trim().equals("")
							&& FileUtils.fileExist(carimboPath.substring(2))) {
						resposta = carimboPath.substring(2);
					}
				}
				carimboPath = br.readLine();
			}
			return resposta;
		}
	}
}
