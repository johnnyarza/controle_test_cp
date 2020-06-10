package gui;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.net.URL;
import java.util.ResourceBundle;

import gui.util.Alerts;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.stage.FileChooser;
import javafx.stage.Stage;

public class ReportConfigViewController implements Initializable{
	
	private String logoPath;
	
	private String carimboPath;
	
	private File imagesProp;
	
	@FXML
	private Button btSave;
	
	@FXML
	private Button btLogo;
	
	@FXML
	private Button btCarimbo;
	
	@FXML
	private ImageView imgLogo;
	
	@FXML
	private ImageView imgCarimbo;
	
	@FXML
	public void onBtSaveAction() {

	}
	
	private void saveImagePaths() {
		File file = imagesProp;
		try (BufferedWriter bw = new BufferedWriter(new FileWriter(file))) {
			if (!file.exists() || file == null) {
				throw new FileNotFoundException("Archivo ReportImage.properties no encontrado");
			}
			bw.write(logoPath == null || logoPath.trim().equals("") ? "" : "1:" + logoPath);
			bw.newLine();
			bw.write(carimboPath == null || carimboPath.trim().equals("")? "" : "2:" + carimboPath);
		} catch (IOException e) {
			Alerts.showAlert("Error", "IOException", e.getMessage(), AlertType.ERROR);
		} 
		
	}
	
	
	@FXML
	public void onBtLogoAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			logoPath = Utils.getFileAbsolutePath(parentStage, new FileChooser.ExtensionFilter("Imagenes", "*.jpg;*.png"));
			imgLogo.setImage(Utils.createImage(logoPath));
			saveImagePaths();
		} catch (FileNotFoundException e) {
			Alerts.showAlert("Error", "FileNotFoundException", e.getMessage(), AlertType.ERROR);
		}
	}
	
	@FXML
	public void onBtCarimboAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			carimboPath = Utils.getFileAbsolutePath(parentStage, new FileChooser.ExtensionFilter("Imagenes", "*.jpg;*.png"));
			imgCarimbo.setImage(Utils.createImage(carimboPath));
			saveImagePaths();
		} catch (FileNotFoundException e) {
			Alerts.showAlert("Error", "FileNotFoundException", e.getMessage(), AlertType.ERROR);
		}
	}
	

	@Override
	public void initialize(URL location, ResourceBundle resources) {
		initializeNodes();
	}

	private void initializeNodes() {
		setImageSearchButtonGraphics();		
	}
	
	public void loadImages() {
		File file = imagesProp;
		String line;
		try (BufferedReader br = new BufferedReader(new FileReader(file))) {
			
			if (!file.isFile()) {
				throw new FileNotFoundException("Archivo ReportImage.properties no encontrado");
			}
			line = br.readLine();
			while (line != null) {
				if (line.substring(0, 2).equals("1:")) {
					logoPath = line.substring(2);
				}
				if (line.substring(0, 2).equals("2:")) {
					carimboPath = line.substring(2);
				}
				line = br.readLine();
			}
			setImagesInView ();

		} catch (IOException e) {
			Alerts.showAlert("Error", "IOException", e.getMessage(), AlertType.ERROR);
		}
	}
	
	private void setImagesInView () throws IOException  {
		try {
		imgLogo.setImage(logoPath != null && !logoPath.trim().equals("") ? Utils.createImage(logoPath.substring(2))
				: new Image(ReportConfigViewController.class.getResourceAsStream("/images/logo.png")));

		imgCarimbo.setImage(carimboPath != null && !carimboPath.trim().equals("") ? Utils.createImage(carimboPath)
				: new Image(ReportConfigViewController.class.getResourceAsStream("/images/carimbo.png")));
		} catch (IOException e) {
			
			imgLogo.setImage(new Image(ReportConfigViewController.class.getResourceAsStream("/images/logo.png")));
			imgCarimbo.setImage(new Image(ReportConfigViewController.class.getResourceAsStream("/images/carimbo.png")));
			
			try (BufferedWriter bw = new BufferedWriter(new FileWriter(imagesProp))) {
				//erase file
			} catch (IOException e1) {
				Alerts.showAlert("Error", "IOException", e.getMessage(), AlertType.ERROR);
			}		
			throw e;
		}
	}

	private void setImageSearchButtonGraphics() {
		ImageView imgView = Utils.createImageView("/images/lupa.png", 15.0, 15.0);
		btCarimbo.setGraphic(imgView);	
		ImageView imgView2 = Utils.createImageView("/images/lupa.png", 15.0, 15.0);
		btLogo.setGraphic(imgView2);	
	}

	public String getLogoPath() {
		return logoPath;
	}

	public void setLogoPath(String logoPath) {
		this.logoPath = logoPath;
	}

	public String getCarimboPath() {
		return carimboPath;
	}

	public void setCarimboPath(String carimboPath) {
		this.carimboPath = carimboPath;
	}


	public File getImagesProp() {
		return imagesProp;
	}


	public void setImagesProp(File imagesProp) {
		this.imagesProp = imagesProp;
	}
	
}
