package gui;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.net.URL;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;
import java.util.Properties;
import java.util.ResourceBundle;
import java.util.logging.Level;

import application.log.LogUtils;
import application.util.FileUtils;
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
	
	private LogUtils logger;
	
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
	
	private void saveImagePaths()  {
		try {
		File file = imagesProp;
		Map <String,String> mapProps = new HashMap<>();
		Properties newProps = new Properties();
		
		mapProps.put("logoPath",logoPath == null || logoPath.trim().equals("") ? "" : logoPath);
		mapProps.put("carimboPath",carimboPath == null || carimboPath.trim().equals("")? "" : carimboPath);
		
		newProps.putAll(mapProps);
		newProps.store(new FileOutputStream(file),null);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error in saveImagePaths", "IOException", e.getMessage(), AlertType.ERROR);
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
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error in onBtLogoAction", "FileNotFoundException", e.getMessage(), AlertType.ERROR);
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
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error in onBtCarimboAction", "FileNotFoundException", e.getMessage(), AlertType.ERROR);
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
		try {
			Properties props = loadReportImageProperties();
			logoPath = props.containsKey("logoPath") ? props.getProperty("logoPath") : "";
			carimboPath = props.containsKey("carimboPath") ? props.getProperty("carimboPath") : "";
			setImagesInView ();
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error in loadImages", "IOException", e.getMessage(), AlertType.ERROR);
		}
		
	}
	
	private Properties loadReportImageProperties() throws IOException {
		File file = imagesProp;
		if (file == null || !file.isFile()) {			
			Path path = FileUtils.writeProperties("ReportImage.properties", new HashMap<String, String>());
			file = path.toFile();
			setImagesProp(file);
		}	
		FileInputStream fs = new FileInputStream(file);
		Properties props = new Properties();
		props.load(fs);
		return props;
	}
	
	private void setImagesInView () throws IOException  {
		//
		try {
		imgLogo.setImage(logoPath != null && !logoPath.trim().equals("") ? Utils.createImage(logoPath)
				: new Image(ReportConfigViewController.class.getResourceAsStream("/images/logo.png")));

		imgCarimbo.setImage(carimboPath != null && !carimboPath.trim().equals("") ? Utils.createImage(carimboPath)
				: new Image(ReportConfigViewController.class.getResourceAsStream("/images/carimbo.png")));
		} catch (IOException e) {
			
			imgLogo.setImage(new Image(ReportConfigViewController.class.getResourceAsStream("/images/logo.png")));
			imgCarimbo.setImage(new Image(ReportConfigViewController.class.getResourceAsStream("/images/carimbo.png")));	
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


	public void setImagesProp(File file) {
		this.imagesProp = file;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}
	
}
