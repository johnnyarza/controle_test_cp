package gui.util;

import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.scene.control.TextField;

public class Constraints {

	public static void setTextFieldInteger(TextField txt) {
		ChangeListener<String> setTextFieldInteger = new ChangeListener<>() {
			@Override
			public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
				if (newValue != null && !newValue.matches("\\d*")) {
					txt.setText(oldValue);
				}
			}
		};

		txt.textProperty().addListener(setTextFieldInteger);
		//txt.textProperty().removeListener(setTextFieldInteger);
	}

	public static void setTextFieldMaxLength(TextField txt, int max) {
		txt.textProperty().addListener((obs, oldValue, newValue) -> {
			if (newValue != null && newValue.length() > max) {
				txt.setText(oldValue);
			}
		});
	}

	public static void setTextFieldDouble(TextField txt) {

		txt.textProperty().addListener((obs, oldValue, newValue) -> {
			if (newValue != null && !newValue.matches("\\d*([\\,]\\d*)?")) {
				txt.setText(oldValue);
			}
		});
	}
}