$(document).ready(function() {
    // Arama fonksiyonu
    function performSearch() {
        const searchTerm = $('#liveSearchInput').val().toLowerCase().trim();
        let visibleCount = 0;
        
        $('.product-item').each(function() {
            const productName = $(this).data('name');
            const productCode = $(this).data('code');
            
            if (productName.includes(searchTerm) || productCode.includes(searchTerm)) {
                $(this).show();
                visibleCount++;
            } else {
                $(this).hide();
            }
        });
        
        $('#searchCounter').text(visibleCount + ' ürün bulundu');
        $('#product-count-grid').text(visibleCount + ' ürün bulundu');
        $('#noResultsMessage').toggle(visibleCount === 0 && searchTerm.length > 0);
    }
    
    // Arama inputu event listener
    $('#liveSearchInput').on('input', performSearch);
    
    // Filtre aç/kapa fonksiyonu
    function toggleFilter(id) {
        const element = document.getElementById(id);
        element.classList.toggle('show');
        const arrow = element.previousElementSibling.querySelector('.arrow');
        arrow.textContent = element.classList.contains('show') ? '▼' : '►';
    }
    
    // Filtre kaldırma
    $(document).on('click', '.remove-filter', function() {
        const filter = $(this).data('filter');
        const value = $(this).data('value');
        const url = new URL(window.location.href);
        const params = new URLSearchParams(url.search);
        
        let values = params.get(filter) ? params.get(filter).split(',') : [];
        values = values.filter(v => v !== value.toString());
        
        if(values.length > 0) {
            params.set(filter, values.join(','));
        } else {
            params.delete(filter);
        }
        
        window.location.search = params.toString();
    });
    
    // Tüm filtreleri temizle
    $('#remove-all').click(function() {
        window.location.href = '@Url.Action("List", "Product")';
    });
    
    // Filtre panelini aç
    $('#filterShop').click(function() {
        $('.sidebar-filter').addClass('open');
    });
    
    // Filtre panelini kapat
    $('.close-filter').click(function() {
        $('.sidebar-filter').removeClass('open');
    });
    
    // Sıralama seçeneği
    $('.select-item').click(function(e) {
        e.preventDefault();
        const sortValue = $(this).data('sort-value');
        const url = new URL(window.location.href);
        url.searchParams.set('sortOrder', sortValue);
        window.location.href = url.toString();
    });
    
    // Favori ürünleri yükle
    function loadFavoriteProducts() {
        $.ajax({
            url: '/Shop/Product/GetFavoriteProductIds',
            type: 'GET',
            success: function(response) {
                console.log("AJAX Yanıtı:", response);
                if (response && response.favoriteProductIds) {
                    console.log("Favori Ürün ID'leri:", response.favoriteProductIds);
                    response.favoriteProductIds.forEach(function(productId) {
                        var button = $(".wishlist[data-product-id='" + productId + "']");
                        button.addClass("favorited");
                        button.find("span.fa-heart").removeClass("far").addClass("fas").css("color", "red");
                        button.find(".tooltip").text("Favorilerden Çıkar");
                    });
                }
            },
            error: function(xhr, status, error) {
                console.error("AJAX İsteği Başarısız:", error);
            }
        });
    }
    
    // Favorilere ekle/çıkar fonksiyonu
    window.addToFavorites = function(element) {
        const productId = $(element).data('product-id');
        const isFavorite = $(element).hasClass('favorited');
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Shop/Product/ToggleFavorite',
            type: 'POST',
            data: { 
                productId: productId,
                __RequestVerificationToken: token
            },
            success: function(response) {
                if (response.success) {
                    if (isFavorite) {
                        $(element).removeClass('favorited');
                        $(element).find('span.fa-heart').removeClass('fas').addClass('far').css('color', '');
                        $(element).find('.tooltip').text('Favorilere Ekle');
                    } else {
                        $(element).addClass('favorited');
                        $(element).find('span.fa-heart').removeClass('far').addClass('fas').css('color', 'red');
                        $(element).find('.tooltip').text('Favorilerden Çıkar');
                    }
                } else {
                    alert('Hata: ' + response.message);
                }
            },
            error: function(xhr) {
                if (xhr.status === 401) {
                    alert('Bu işlemi yapmak için giriş yapmalısınız!');
                } else {
                    alert('Bir hata oluştu: ' + xhr.statusText);
                }
            }
        });
    };
    
    // Sayfa yüklendiğinde favori ürünleri yükle
    loadFavoriteProducts();
});